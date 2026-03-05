using Duende.IdentityServer.Models;

namespace IdentityServer;

public static class Config
{
    public static IEnumerable<IdentityResource> IdentityResources =>
        [
            new IdentityResources.OpenId(),
            new IdentityResources.Profile(),
        ];

    public static IEnumerable<ApiScope> ApiScopes =>
        [
            new ApiScope("weather"),
        ];

    public static IEnumerable<Client> GetClients(string? webUrl = "https://localhost:5014")
    {
        return
        [
            // interactive client using code flow + pkce
            new Client
            {
                ClientId = "web",
                ClientSecrets = { new Secret("49C1A7E1-0C79-4A89-A3D6-A37998FB86B0".Sha256()) },

                AllowedGrantTypes = GrantTypes.Code,

                RedirectUris = { $"{webUrl}/signin-oidc" },
                FrontChannelLogoutUri = $"{webUrl}/signout-oidc",
                PostLogoutRedirectUris = { $"{webUrl}/signout-callback-oidc" },

                AllowOfflineAccess = true,
                AllowedScopes = { "openid", "profile", "weather" },

                // Ridiculously short to force renewals. Duende.AccessTokenManagement.OpenIdConnect
                // by defaults renews token 60 seconds before expiry. So this means effectively
                // every 10 seconds.
                AccessTokenLifetime = 70
            },
        ];
    }

}
