// Copyright (c) Duende Software. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using Duende.IdentityServer.Models;

namespace IdentityServerHost;

public static class Resources
{
    public static IEnumerable<IdentityResource> Identity =>
        new IdentityResource[]
        {
            new IdentityResources.OpenId(),
            new IdentityResources.Email(),
        };

    public static IEnumerable<ApiScope> ApiScopes =>
        new ApiScope[]
        {
            new ApiScope("scope1"),
            new ApiScope("scope2"),
        };

    public static IEnumerable<ApiResource> ApiResources =>
        new ApiResource[]
        {
            // this resource is used in the introspection sample
            // for introspection, a api secret is necessary
            // this is one of the features prvovided by API resource (as opposed to plain scopes)
            new ApiResource("resource1")
            {
                Scopes = { "scope2" },

                ApiSecrets =
                {
                    new Secret("secret".Sha256())
                }
            }
        };
}
