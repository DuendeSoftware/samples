// Copyright (c) Duende Software. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.


using System.Collections.Generic;
using Duende.IdentityServer.Models;

namespace IdentityServerHost;

public static class Clients
{
    public static IEnumerable<Client> List =>
        new[]
        {
            // MVC back-channel logout sample
            new Client
            {
                ClientId = "mvc.backchannel.sample",
                ClientSecrets = { new Secret("secret".Sha256()) },

                AllowedGrantTypes = GrantTypes.Code,

                RedirectUris = { "https://localhost:44300/signin-oidc" },
                BackChannelLogoutUri = "https://localhost:44300/logout",
                PostLogoutRedirectUris = { "https://localhost:44300/signout-callback-oidc" },

                AllowOfflineAccess = true,
                AllowedScopes = { "openid", "profile", "scope1", "scope2" },

                // this causes refresh tokens to slide the user's session lifetime at IdentityServer
                CoordinateLifetimeWithUserSession = true,
            },
        };
}
