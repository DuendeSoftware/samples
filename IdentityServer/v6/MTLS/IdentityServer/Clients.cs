// Copyright (c) Duende Software. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.


using System.Collections.Generic;
using Duende.IdentityServer;
using Duende.IdentityServer.Models;

namespace IdentityServerHost;

public static class Clients
{
    public static IEnumerable<Client> List =>
        new[]
        {
            new Client
            {
                ClientId = "mtls",

                ClientSecrets =
                {
                    new Secret("5D9E9B6B333CD42C99D1DE6175CC0F3EF99DDF68")
                    {
                        Type = IdentityServerConstants.SecretTypes.X509CertificateThumbprint
                    },
                },

                AllowedGrantTypes = GrantTypes.CodeAndClientCredentials,

                RedirectUris = { "https://localhost:44301/signin-oidc" },
                FrontChannelLogoutUri = "https://localhost:44301/signout-oidc",
                PostLogoutRedirectUris = { "https://localhost:44301/signout-callback-oidc" },

                AllowOfflineAccess = true,
                AllowedScopes = { "openid", "profile", "scope1" }
            },
        };
}
