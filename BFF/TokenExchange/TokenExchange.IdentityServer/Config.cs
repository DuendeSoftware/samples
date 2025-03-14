// Copyright (c) Duende Software. All rights reserved.
// See LICENSE in the project root for license information.


using Duende.IdentityServer.Models;
using IdentityModel;

namespace TokenExchange.IdentityServer
{
    public static class Config
    {
        public static IEnumerable<IdentityResource> IdentityResources =>
            new IdentityResource[]
            {
                new IdentityResources.OpenId(),
                new IdentityResources.Profile(),
            };

        public static IEnumerable<ApiScope> ApiScopes =>
            new ApiScope[]
            {
                new ApiScope("api", new[] { "name" })
            };


        public static IEnumerable<Client> Clients =>
            new Client[]
            {
                new Client
                {
                    ClientId = "spa",
                    ClientSecrets = { new Secret("secret".Sha256()) },
                    
                    AllowedGrantTypes = 
                    { 
                        GrantType.AuthorizationCode, 
                        GrantType.ClientCredentials,
                        OidcConstants.GrantTypes.TokenExchange 
                    },

                    RedirectUris = { "https://localhost:6001/signin-oidc" },
                    
                    BackChannelLogoutUri = "https://localhost:6001/bff/backchannel",
                    
                    PostLogoutRedirectUris = { "https://localhost:6001/signout-callback-oidc" },

                    AllowOfflineAccess = true,
                    AllowedScopes = { "openid", "profile", "api" },
                },
            };
    }
}