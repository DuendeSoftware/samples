// Copyright (c) Duende Software. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using Duende.IdentityServer.Models;

namespace IdentityServerHost;

public static class Clients
{
    public static IEnumerable<Client> List =>
        new[]
        {


            // MVC basic sample
            new Client
            {
                ClientId = "interactive.mvc.sample",
                ClientSecrets = { new Secret("secret".Sha256()) },

                AllowedGrantTypes = GrantTypes.Code,

                RedirectUris = { "https://localhost:44300/signin-oidc" },
                FrontChannelLogoutUri = "https://localhost:44300/signout-oidc",
                PostLogoutRedirectUris = { "https://localhost:44300/signout-callback-oidc" },

                AllowOfflineAccess = true,
                AllowedScopes = { "openid", "email", "scope1", "scope2" }
            },


        };
}
