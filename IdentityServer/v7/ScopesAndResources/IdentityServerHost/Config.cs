// Copyright (c) Duende Software. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.


using Duende.IdentityServer.Models;

namespace IdentityServerHost;

public static class Config
{
    public static readonly IEnumerable<ApiScope> Scopes =
        new[]
        {
            // resource specific scopes
            new ApiScope("resource1.scope1"),
            new ApiScope("resource1.scope2"),

            new ApiScope("resource2.scope1"),
            new ApiScope("resource2.scope2"),

            new ApiScope("resource3.scope1"),
            new ApiScope("resource3.scope2"),
            
            // a scope without resource association
            new ApiScope("scope3"),
            new ApiScope("scope4"),
            
            // a scope shared by multiple resources
            new ApiScope("shared.scope"),

            // a parameterized scope
            new ApiScope("transaction", "Transaction")
        };

    // API resources are more formal representation of a resource with processing rules and their scopes (if any)
    public static readonly IEnumerable<ApiResource> Resources =
        new[]
        {
            new ApiResource("urn:resource1", "Resource 1")
            {
                Scopes = { "resource1.scope1", "resource1.scope2", "shared.scope" }
            },

            new ApiResource("urn:resource2", "Resource 2")
            {
                Scopes = { "resource2.scope1", "resource2.scope2", "shared.scope" }
            },

            new ApiResource("urn:resource3", "Resource 3 (isolated)")
            {
                Scopes = { "resource3.scope1", "resource3.scope2", "shared.scope" },

                RequireResourceIndicator = true
            }
        };

    public static IEnumerable<Client> Clients =>
        new Client[]
        {
            new Client
            {
                ClientId = "resources.and.scopes",
                ClientSecrets = { new Secret("secret".Sha256()) },
                ClientClaimsPrefix = "",

                AllowedGrantTypes = GrantTypes.ClientCredentials,
                AllowedScopes =
                {
                    "resource1.scope1",
                    "resource1.scope2",

                    "resource2.scope1",
                    "resource2.scope2",

                    "resource3.scope1",
                    "resource3.scope2",

                    "shared.scope",

                    "scope3",
                    "scope4",

                    "transaction"
                }
            }
        };
}
