using Duende.IdentityServer.Models;

namespace McpDemo.IdentityServer;

public static class Config
{
    public static IEnumerable<IdentityResource> IdentityResources =>
    [
        new IdentityResources.OpenId(),
        new IdentityResources.Profile()
    ];

    public static IEnumerable<ApiResource> ApiResources =>
    [
        new("https://localhost:7141/", "MCP Server")
        {
            Scopes = { "mcp:tools" }
        }
    ];

    public static IEnumerable<ApiScope> ApiScopes =>
    [
        new("mcp:tools")
    ];
}
