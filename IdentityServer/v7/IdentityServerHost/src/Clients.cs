// Copyright (c) Duende Software. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.


using Duende.IdentityServer;
using Duende.IdentityServer.Models;

namespace IdentityServerHost;

public static class Clients
{
    public static IEnumerable<Client> List =>
        new[]
        {
            // client credentials flow sample
            new Client
            {
                ClientId = "client.credentials.sample",

                AllowedGrantTypes = GrantTypes.ClientCredentials,
                ClientSecrets = { new Secret("secret".Sha256()) },

                AllowedScopes = { "scope1", "scope2" }
            },

            // JWT-based client authentication sample
            new Client
            {
                ClientId = "jwt.client.credentials.sample",

                AllowedGrantTypes = GrantTypes.ClientCredentials,

                // this client uses an RSA key as client secret
                // and https://docs.duendesoftware.com/identityserver/tokens/client-authentication
                ClientSecrets =
                {
                    new Secret
                    {
                        Type = IdentityServerConstants.SecretTypes.JsonWebKey,
                        Value = """
                        {
                            "e":"AQAB",
                            "kid":"ZzAjSnraU3bkWGnnAqLapYGpTyNfLbjbzgAPbbW2GEA",
                            "kty":"RSA",
                            "n":"wWwQFtSzeRjjerpEM5Rmqz_DsNaZ9S1Bw6UbZkDLowuuTCjBWUax0vBMMxdy6XjEEK4Oq9lKMvx9JzjmeJf1knoqSNrox3Ka0rnxXpNAz6sATvme8p9mTXyp0cX4lF4U2J54xa2_S9NF5QWvpXvBeC4GAJx7QaSw4zrUkrc6XyaAiFnLhQEwKJCwUw4NOqIuYvYp_IXhw-5Ti_icDlZS-282PcccnBeOcX7vc21pozibIdmZJKqXNsL1Ibx5Nkx1F1jLnekJAmdaACDjYRLL_6n3W4wUp19UvzB1lGtXcJKLLkqB6YDiZNu16OSiSprfmrRXvYmvD8m6Fnl5aetgKw"
                        }
                        """
                    }
                },

                AllowedScopes = { "scope1", "scope2" }
            },

            // introspection sample
            new Client
            {
                ClientId = "introspection.sample",

                AllowedGrantTypes = GrantTypes.ClientCredentials,
                ClientSecrets = { new Secret("secret".Sha256()) },

                AccessTokenType = AccessTokenType.Reference,

                AllowedScopes = { "scope1", "scope2" }
            },

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
                AllowedScopes = { "openid", "profile", "scope1", "scope2" }
            },

            // MVC basic sample with token management
            // this client has a short access token lifetime to experiment with automatic refresh
            new Client
            {
                ClientId = "interactive.mvc.sample.short.token.lifetime",
                ClientSecrets = { new Secret("secret".Sha256()) },

                AllowedGrantTypes = GrantTypes.Code,
                AccessTokenLifetime = 75,

                RedirectUris = { "https://localhost:44300/signin-oidc" },
                FrontChannelLogoutUri = "https://localhost:44300/signout-oidc",
                PostLogoutRedirectUris = { "https://localhost:44300/signout-callback-oidc" },

                AllowOfflineAccess = true,
                AllowedScopes = { "openid", "profile", "scope1", "scope2" }
            },

            // MVC sample using JAR (signed authorize requests) and JWTs for client authentication
            new Client
            {
                ClientId = "interactive.mvc.sample.jarjwt",

                // force client to use signed authorize request
                RequireRequestObject = true,

                // this client uses an RSA key as client secret
                // this key is used for both validating the signature on the authorize request
                // and for client authentication
                // see https://docs.duendesoftware.com/identityserver/v5/advanced/jar/
                // and https://docs.duendesoftware.com/identityserver/tokens/client-authentication
                ClientSecrets =
                {
                    new Secret
                    {
                        Type = IdentityServerConstants.SecretTypes.JsonWebKey,
                        Value = """
                        {
                            "e":"AQAB",
                            "kid":"ZzAjSnraU3bkWGnnAqLapYGpTyNfLbjbzgAPbbW2GEA",
                            "kty":"RSA",
                            "n":"wWwQFtSzeRjjerpEM5Rmqz_DsNaZ9S1Bw6UbZkDLowuuTCjBWUax0vBMMxdy6XjEEK4Oq9lKMvx9JzjmeJf1knoqSNrox3Ka0rnxXpNAz6sATvme8p9mTXyp0cX4lF4U2J54xa2_S9NF5QWvpXvBeC4GAJx7QaSw4zrUkrc6XyaAiFnLhQEwKJCwUw4NOqIuYvYp_IXhw-5Ti_icDlZS-282PcccnBeOcX7vc21pozibIdmZJKqXNsL1Ibx5Nkx1F1jLnekJAmdaACDjYRLL_6n3W4wUp19UvzB1lGtXcJKLLkqB6YDiZNu16OSiSprfmrRXvYmvD8m6Fnl5aetgKw"
                        }
                        """
                    }
                },

                AllowedGrantTypes = GrantTypes.Code,

                RedirectUris = { "https://localhost:44300/signin-oidc" },
                FrontChannelLogoutUri = "https://localhost:44300/signout-oidc",
                PostLogoutRedirectUris = { "https://localhost:44300/signout-callback-oidc" },

                AllowOfflineAccess = true,
                AllowedScopes = { "openid", "profile", "scope1", "scope2" }
            },

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
                AllowedScopes = { "openid", "profile", "scope1", "scope2" }
            },

            ///////////////////////////////////////////
            // MVC PAR Sample
            //////////////////////////////////////////
            new Client
            {
                ClientId = "mvc.par",
                ClientName = "MVC PAR Client",

                ClientSecrets =
                {
                    new Secret("secret".Sha256())
                },

                RequireRequestObject = false,

                AllowedGrantTypes = GrantTypes.Code,

                RequirePushedAuthorization = true,

                // Note that redirect uris are optional for PAR clients when the
                // AllowUnregisteredPushedRedirectUris flag is enabled
                // RedirectUris = { "https://localhost:44300/signin-oidc" },

                FrontChannelLogoutUri = "https://localhost:44300/signout-oidc",
                PostLogoutRedirectUris = { "https://localhost:44300/signout-callback-oidc" },

                AllowOfflineAccess = true,

                AllowedScopes = { "openid", "profile", "scope1", "scope2" }
            },
        };
}
