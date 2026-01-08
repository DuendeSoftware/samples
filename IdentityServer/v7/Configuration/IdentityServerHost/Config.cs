// Copyright (c) Duende Software. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using Duende.IdentityServer.Models;

namespace IdentityServer;

public static class Config
{
    public static IEnumerable<IdentityResource> IdentityResources =>
    [
        new IdentityResources.OpenId(),
        new IdentityResources.Profile()
    ];

    public static IEnumerable<ApiScope> ApiScopes =>
    [
        new ApiScope("IdentityServer.Configuration"),
        new ApiScope("IdentityServer.Configuration:SetClientSecret"),
        new ApiScope("SimpleApi")
    ];

    public static IEnumerable<Client> Clients =>
    [
        new()
            {
                ClientId = "client",
                ClientName = "Client Credentials Client for DCR",

                AllowedGrantTypes = GrantTypes.ClientCredentials,
                ClientSecrets = { new Secret("secret".Sha256()) },

                AllowedScopes = { "IdentityServer.Configuration", "IdentityServer.Configuration:SetClientSecret" }
            }

    ];

    //Needed for PipelineRegistration sample
    public static IEnumerable<ApiResource> ApiResources =>
    [
        new("configuration", "IdentityServer.Configuration API")
            {
                Scopes = { "IdentityServer.Configuration" },
                ApiSecrets = { new Secret("secret".Sha256()) }
            }
    ];
}
