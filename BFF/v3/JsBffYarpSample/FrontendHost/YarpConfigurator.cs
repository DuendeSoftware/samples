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
                RouteId = "todos",
                ClusterId = "cluster1",

                Match = new RouteMatch
                {
                    Path = "/todos/{**catch-all}"
                }
            }.WithAccessToken(TokenType.User),
                ],
                [
            new ClusterConfig
            {
                ClusterId = "cluster1",

                Destinations = new Dictionary<string, DestinationConfig>(StringComparer.OrdinalIgnoreCase)
                {
                    { "destination1", new DestinationConfig() { Address = "https://localhost:5020" } },
                }
            }
        ]);
    }
}

