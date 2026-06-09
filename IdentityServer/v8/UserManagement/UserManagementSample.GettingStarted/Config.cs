// Copyright (c) Duende Software. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using Duende.IdentityServer.Models;

namespace UserManagementSample.GettingStarted;

public static class Config
{
    public static IEnumerable<IdentityResource> IdentityResources =>
    [
        new IdentityResources.OpenId(),
        new IdentityResources.Profile(),
        new IdentityResources.Email(),
    ];

    public static IEnumerable<ApiScope> ApiScopes =>
    [
        new ApiScope("api", "My API"),
    ];

    public static IEnumerable<Client> Clients =>
    [
        new Client
        {
            ClientId = "interactive",
            ClientSecrets = { new Secret("secret".Sha256()) },
            AllowedGrantTypes = GrantTypes.Code,
            RedirectUris = { "https://localhost:5002/signin-oidc" },
            PostLogoutRedirectUris = { "https://localhost:5002/signout-callback-oidc" },
            AllowedScopes = { "openid", "profile", "email", "api" },
        },
    ];
}
