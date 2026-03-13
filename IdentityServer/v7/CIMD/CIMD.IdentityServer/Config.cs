using Duende.IdentityServer.Models;

namespace CIMD.IdentityServer;

public static class Config
{
    public static IEnumerable<IdentityResource> IdentityResources =>
    [
        new IdentityResources.OpenId(),
        new IdentityResources.Profile()
    ];

    public static IEnumerable<ApiResource> ApiResources =>
    [
        new("https://localhost:7241", "MCP Server")
        {
            Scopes = { "mcp" }
        }
    ];

    public static IEnumerable<ApiScope> ApiScopes =>
    [
        new("mcp")
    ];

    /// <summary>
    /// Statically configured clients. These coexist with CIMD clients, which
    /// are resolved dynamically by <see cref="CimdClientStore"/>.
    /// </summary>
    public static IEnumerable<Client> Clients =>
    [
        new()
        {
            ClientId = "m2m",
            ClientSecrets = { new Secret("secret".Sha256()) },
            AllowedGrantTypes = GrantTypes.ClientCredentials,
            AllowedScopes = { "mcp" }
        }
    ];
}
