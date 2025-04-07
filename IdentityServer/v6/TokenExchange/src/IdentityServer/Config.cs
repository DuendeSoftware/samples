// Copyright (c) Duende Software. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.


using System.Collections.Generic;
using Duende.IdentityServer.Models;
using IdentityModel;

namespace IdentityServerHost;

public static class Config
{
    public static readonly IEnumerable<ApiScope> Scopes =
        new[]
        {
            new ApiScope("scope1"),
            new ApiScope("scope2"),
        };

    public static IEnumerable<Client> Clients =>
        new[]
        {
            // represent the front end client
            new Client
            {
                ClientId = "front.end",
                ClientSecrets = { new Secret("secret".Sha256()) },

                AllowedGrantTypes = GrantTypes.ClientCredentials,
                AllowedScopes = { "scope1" },
                
                // simulate interactive user
                ClientClaimsPrefix = "",
                Claims =
                {
                    new ClientClaim("sub", "123")
                }
            },
            
            // represents the client that is delegating the access token
            new Client
            {
                ClientId = "api1",
                ClientSecrets = { new Secret("secret".Sha256()) },

                AllowedGrantTypes = { OidcConstants.GrantTypes.TokenExchange },
                AllowedScopes = { "scope2" }
            }
        };
}
