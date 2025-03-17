using Duende.Bff;
using Duende.Bff.Yarp;
using Yarp.ReverseProxy.Configuration;

public static class YarpConfigurator
{
    public static void Configure(this IReverseProxyBuilder builder)
    {
        builder.LoadFromMemory(
        [
            new RouteConfig()
            {
                RouteId = "user-token",
                ClusterId = "cluster1",

                Match = new()
                {
                    Path = "/yarp/user-token/{**catch-all}"
                }
            }.WithAccessToken(TokenType.User).WithAntiforgeryCheck(),
            new RouteConfig()
            {
                RouteId = "client-token",
                ClusterId = "cluster1",

                Match = new()
                {
                    Path = "/yarp/client-token/{**catch-all}"
                }
            }.WithAccessToken(TokenType.Client).WithAntiforgeryCheck(),
            new RouteConfig()
            {
                RouteId = "user-or-client-token",
                ClusterId = "cluster1",

                Match = new()
                {
                    Path = "/yarp/user-or-client-token/{**catch-all}"
                }
            }.WithAccessToken(TokenType.UserOrClient).WithAntiforgeryCheck(),
            new RouteConfig()
            {
                RouteId = "anonymous",
                ClusterId = "cluster1",

                Match = new()
                {
                    Path = "/yarp/anonymous/{**catch-all}"
                }
            }.WithAntiforgeryCheck()
        ],
        [
            new ClusterConfig
            {
                ClusterId = "cluster1",

                Destinations = new Dictionary<string, DestinationConfig>(StringComparer.OrdinalIgnoreCase)
                {
                    { "destination1", new() { Address = "https://localhost:6001" } },
                }
            }
        ]);
    }

    public static void MapRemoteUrls(this WebApplication app)
    {
        // On this path, we use a client credentials token
        app.MapRemoteBffApiEndpoint("/api/client-token", "https://localhost:6001")
            .RequireAccessToken(TokenType.Client);

        // On this path, we use a user token if logged in, and fall back to a client credentials token if not
        app.MapRemoteBffApiEndpoint("/api/user-or-client-token", "https://localhost:6001")
            .RequireAccessToken(TokenType.UserOrClient);

        // On this path, we make anonymous requests
        app.MapRemoteBffApiEndpoint("/api/anonymous", "https://localhost:6001");

        // On this path, we use the client token only if the user is logged in
        app.MapRemoteBffApiEndpoint("/api/optional-user-token", "https://localhost:6001")
            .WithOptionalUserAccessToken();

        // On this path, we require the user token
        app.MapRemoteBffApiEndpoint("/api/user-token", "https://localhost:6001")
            .RequireAccessToken(TokenType.User);
    }
}

