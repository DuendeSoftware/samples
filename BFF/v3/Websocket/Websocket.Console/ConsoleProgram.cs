using System.Net.WebSockets;
using System.Text;
using System.Text.Json;

namespace GraphQL.Client.Tests
{
    public class ConsoleProgram
    {
        /// <summary>
        /// This console application connects to a GraphQL WebSocket endpoint,
        /// initiates a subscription, and prints any received messages to the console.
        /// </summary>
        public static async Task Main(string[] args)
        {
            //var serverUri = new Uri("ws://localhost:5095/graphql");
            var serverUri = new Uri("ws://localhost:5197/graphql");
            using var cts = new CancellationTokenSource();
            Console.CancelKeyPress += (sender, e) =>
            {
                e.Cancel = true;
                Console.WriteLine("Ctrl+C detected. Shutting down.");
                cts.Cancel();
            };

            while (!cts.IsCancellationRequested)
            {
                using var client = new ClientWebSocket();
                client.Options.AddSubProtocol("graphql-ws");

                try
                {
                    Console.WriteLine($"Connecting to {serverUri}...");
                    await client.ConnectAsync(serverUri, cts.Token);
                    Console.WriteLine("Connection established.");

                    var initRequest = new { type = "connection_init" };
                    await SendMessageAsync(client, initRequest, cts.Token);
                    Console.WriteLine("Sent connection_init message.");

                    var ackMessage = await ReceiveMessageAsync(client, cts.Token);
                    if (!ackMessage.Contains("\"type\":\"connection_ack\"", StringComparison.OrdinalIgnoreCase))
                    {
                        Console.WriteLine("Server did not acknowledge the connection.");
                        Console.WriteLine("Waiting 3 seconds before reconnecting...");
                        await Task.Delay(3000, CancellationToken.None);
                        continue;
                    }
                    Console.WriteLine("Connection acknowledged by server.");

                    var subscriptionQuery = "subscription { bookAdded { title } }";
                    var startRequest = new
                    {
                        id = "1",
                        type = "start",
                        payload = new { query = subscriptionQuery }
                    };
                    await SendMessageAsync(client, startRequest, cts.Token);
                    Console.WriteLine("Sent subscription 'start' message.");

                    Console.WriteLine("Listening for subscription data... Press Ctrl+C to exit.");
                    while (client.State == WebSocketState.Open && !cts.IsCancellationRequested)
                    {
                        var receivedData = await ReceiveMessageAsync(client, cts.Token);
                        if (string.IsNullOrEmpty(receivedData))
                        {
                            Console.WriteLine("Connection closed by server. Attempting to reconnect...");
                            break;
                        }
                        if (!receivedData.Contains("\"type\":\"ka\""))
                        {
                            Console.WriteLine($"Received: {receivedData}");
                        }
                    }
                }
                catch (OperationCanceledException)
                {
                    break;
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"An error occurred: {ex.Message}");
                }

                if (!cts.IsCancellationRequested)
                {
                    Console.WriteLine("Waiting 3 seconds before reconnecting...");
                    await Task.Delay(3000, CancellationToken.None);
                }
            }

            Console.WriteLine("Application exiting.");
        }

        /// <summary>
        /// Helper method to serialize an object to JSON and send it over the WebSocket.
        /// </summary>
        private static async Task SendMessageAsync(ClientWebSocket client, object message, CancellationToken token)
        {
            var jsonMessage = JsonSerializer.Serialize(message, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });
            var messageBuffer = Encoding.UTF8.GetBytes(jsonMessage);
            await client.SendAsync(new ArraySegment<byte>(messageBuffer), WebSocketMessageType.Text, true, token);
        }

        /// <summary>
        /// Helper method to listen for a single message from the WebSocket.
        /// </summary>
        private static async Task<string> ReceiveMessageAsync(ClientWebSocket client, CancellationToken token)
        {
            var buffer = new ArraySegment<byte>(new byte[2048]);
            WebSocketReceiveResult result;
            do
            {
                result = await client.ReceiveAsync(buffer, token);
                // The server can send "keep-alive" messages, which we ignore.
                // We are waiting for a message with actual content (EndOfMessage = true).
            } while (!result.EndOfMessage && !token.IsCancellationRequested);

            if (result.MessageType == WebSocketMessageType.Close)
            {
                return null;
            }

            return Encoding.UTF8.GetString(buffer.Array, 0, result.Count);
        }
    }
}

