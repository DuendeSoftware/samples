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
            // WebForms basic sample
            new Client
            {
                ClientId = "interactive.webforms.sample",
                ClientName = "WebForms Sample",
                ClientSecrets = { new Secret("secret".Sha256()) },

                AllowedGrantTypes = GrantTypes.Code,

                RedirectUris = { "https://localhost:44302/" },
                PostLogoutRedirectUris = { "https://localhost:44302/" },

                AllowOfflineAccess = true,
                AllowedScopes = { "openid", "profile", "scope1" },
            },
            // MVC sample built with OWIN on .NET framework 4.8
            new Client
            {
                ClientId = "interactive.mvc.owin.sample",
                ClientName = "MVC Sample",
                ClientSecrets = { new Secret("secret".Sha256()) },

                AllowedGrantTypes = GrantTypes.Code,

                RedirectUris = { "https://localhost:44301/" },
                PostLogoutRedirectUris = { "https://localhost:44301/" },

                AllowOfflineAccess = true,
                AllowedScopes = { "openid", "profile", "scope1" },
            },
        };
}
