// Copyright (c) Duende Software. All rights reserved.
// See LICENSE in the project root for license information.

using Duende.IdentityModel;
using Duende.IdentityServer.Models;

namespace MultiFrontendSSO.IdentityServer;

public static class Config
{
    public static IEnumerable<IdentityResource> IdentityResources =>
    [
        new IdentityResources.OpenId(),
        new IdentityResources.Profile(),
        new IdentityResources.Email(),
        new()
        {
            Name = JwtClaimTypes.Roles,
            UserClaims = new List<string>
            {
                JwtClaimTypes.Roles
            }
        }
    ];

    public static IEnumerable<ApiScope> ApiScopes =>
    [
        new("scope1"),
        new("scope2")
    ];

    public static IEnumerable<ApiResource> ApiResources =>
    [
        new(
            "roles",
            new List<string>
            {
                JwtClaimTypes.Roles
            })
    ];

    public static IEnumerable<Client> Clients =>
    [
        new()
        {
            ClientId = "frontend1",
            ClientSecrets = { new Secret("secret".Sha256()) },
            AllowedGrantTypes = GrantTypes.Code,
            RequirePkce = true,

            RedirectUris = { "https://localhost:5002/frontend1/bff/signin-oidc" },
            FrontChannelLogoutUri = "https://localhost:5002/frontend1/bff/signout-oidc",
            PostLogoutRedirectUris = { "https://localhost:5002/frontend1/bff/signout-callback-oidc" },
            BackChannelLogoutUri = "https://localhost:5002/frontend1/bff/backchannel",
            BackChannelLogoutSessionRequired = true,

            AllowOfflineAccess = true,
            AllowedScopes = { "openid", JwtClaimTypes.Profile, JwtClaimTypes.Email, JwtClaimTypes.Roles }
        },
        new()
        {
            ClientId = "frontend2",
            ClientSecrets = { new Secret("secret".Sha256()) },
            AllowedGrantTypes = GrantTypes.Code,
            RequirePkce = true,

            RedirectUris = { "https://localhost:5002/frontend2/bff/signin-oidc" },
            FrontChannelLogoutUri = "https://localhost:5002/frontend2/bff/signout-oidc",
            PostLogoutRedirectUris = { "https://localhost:5002/frontend2/bff/signout-callback-oidc" },
            BackChannelLogoutUri = "https://localhost:5002/frontend2/bff/backchannel",
            BackChannelLogoutSessionRequired = true,

            AllowOfflineAccess = true,
            AllowedScopes = { "openid", JwtClaimTypes.Profile, JwtClaimTypes.Email, JwtClaimTypes.Roles }
        }
    ];
}
