using System.Net.WebSockets;
using System.Text;
using System.Text.Json;

namespace GraphQL.Client.Tests
{
    public class Program
    {
        /// <summary>
        /// This console application connects to a GraphQL WebSocket endpoint,
        /// initiates a subscription, and prints any received messages to the console.
        /// </summary>
        public static async Task Main(string[] args)
        {
            // --- Arrange ---
            using var client = new ClientWebSocket();
            // Hot Chocolate and other GraphQL servers use the 'graphql-ws' sub-protocol.
            client.Options.AddSubProtocol("graphql-ws");

            // Define the server URI.
            var serverUri = new Uri("ws://localhost:5197/graphql");

            // Use a CancellationTokenSource to handle graceful shutdown on Ctrl+C.
            using var cts = new CancellationTokenSource();
            Console.CancelKeyPress += (sender, e) =>
            {
                e.Cancel = true; // Prevent the process from terminating immediately.
                Console.WriteLine("Ctrl+C detected. Shutting down.");
                cts.Cancel();
            };


            // --- Main Logic ---
            try
            {
                // 1. Connect to the server.
                Console.WriteLine($"Connecting to {serverUri}...");
                await client.ConnectAsync(serverUri, cts.Token);
                Console.WriteLine("Connection established.");

                // 2. Send the connection initialization message.
                // This is the first message the client must send after the WebSocket is open.
                var initRequest = new { type = "connection_init" };
                await SendMessageAsync(client, initRequest, cts.Token);
                Console.WriteLine("Sent connection_init message.");

                // 3. Wait for the 'connection_ack' message from the server.
                // The server will acknowledge the connection before accepting any operations.
                var ackMessage = await ReceiveMessageAsync(client, cts.Token);
                if (ackMessage == null || !ackMessage.Contains("\"type\":\"connection_ack\"", StringComparison.OrdinalIgnoreCase))
                {
                    Console.WriteLine("Server did not acknowledge the connection. Exiting.");
                    return;
                }
                Console.WriteLine("Connection acknowledged by server.");

                // 4. Send the subscription 'start' message.
                var subscriptionQuery = "subscription { bookAdded { title } }";
                var startRequest = new
                {
                    id = "1", // A unique ID for this subscription.
                    type = "start",
                    payload = new { query = subscriptionQuery }
                };
                await SendMessageAsync(client, startRequest, cts.Token);
                Console.WriteLine("Sent subscription 'start' message.");

                // 5. Listen continuously for data from the subscription.
                Console.WriteLine("Listening for subscription data... Press Ctrl+C to exit.");
                while (client.State == WebSocketState.Open && !cts.IsCancellationRequested)
                {
                    var receivedData = await ReceiveMessageAsync(client, cts.Token);
                    // If we receive data (and it's not a keep-alive message), print it.
                    if (!string.IsNullOrEmpty(receivedData) && !receivedData.Contains("\"type\":\"ka\""))
                    {
                        Console.WriteLine($"Received: {receivedData}");
                    }
                }
            }
            catch (OperationCanceledException)
            {
                // This is expected when Ctrl+C is pressed. The application will then proceed to the finally block.
            }
            catch (Exception ex)
            {
                Console.WriteLine($"An error occurred: {ex.Message}");
            }
            finally
            {
                // Ensure the connection is closed gracefully.
                if (client.State == WebSocketState.Open)
                {
                    // 6. Stop the subscription and clean up.
                    // It's good practice to tell the server you are no longer listening.
                    var stopRequest = new { id = "1", type = "stop" };
                    await SendMessageAsync(client, stopRequest, CancellationToken.None);
                    Console.WriteLine("Sent 'stop' message.");

                    await client.CloseAsync(WebSocketCloseStatus.NormalClosure, "Client closing connection", CancellationToken.None);
                    Console.WriteLine("WebSocket connection closed.");
                }
            }
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

