// Copyright (c) Duende Software. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using Duende.IdentityServer.Models;

namespace IdentityServerHost;

public static class Config
{
    private static IdentityResources.OpenId _openid;

    static Config()
    {
        _openid = new IdentityResources.OpenId();
        _openid.UserClaims.Add("acr");
    }

    public static IEnumerable<IdentityResource> IdentityResources =>
        new IdentityResource[]
        {
            _openid,
            new IdentityResources.Profile(),
        };

    public static IEnumerable<ApiScope> ApiScopes =>
        new ApiScope[]
        {
            new ApiScope("scope1", new[]{ "acr" }),
            new ApiScope("scope2", new[]{ "acr" }),
        };

    public static IEnumerable<Client> Clients =>
        new Client[]
        {
            new Client
            {
                ClientId = "step-up",
                ClientName = "Step Up Demo",
                ClientSecrets = { new Secret("secret".Sha256()) },

                AllowedGrantTypes = GrantTypes.Code,

                RedirectUris = { "https://localhost:6001/signin-oidc" },
                FrontChannelLogoutUri = "https://localhost:6001/signout-oidc",
                PostLogoutRedirectUris = { "https://localhost:6001/signout-callback-oidc" },

                AllowedScopes = { "openid", "profile", "scope1" }
            },
        };
}
