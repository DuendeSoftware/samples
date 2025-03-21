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
            ///////////////////////////////////////////
            // CIBA Sample
            //////////////////////////////////////////
            new Client
            {
                ClientId = "ciba",
                ClientName = "CIBA Client",
                ClientSecrets = { new Secret("secret".Sha256()) },
                AllowedGrantTypes = GrantTypes.Ciba,
                RequireConsent = true,
                AllowOfflineAccess = true,
                AllowedScopes =
                {
                    IdentityServerConstants.StandardScopes.OpenId,
                    IdentityServerConstants.StandardScopes.Profile,
                    "scope1",
                    "scope1"
                }
            },
        };
}
