// Copyright (c) Duende Software. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.


using System.Security.Cryptography.X509Certificates;
using Duende.IdentityServer;
using Duende.IdentityServer.Models;

namespace IdentityServerHost;

public static class Clients
{
    // These ClientCert related helper methods make the demo easy to run, but 
    // are not suitable for production. The point is client authentication based
    // on the mTLS certificate needs some way of identifying the certificate 
    // to use, which can either be the client certificates subject or thumbprint.
    // The thumbprint is more specific: it uniquely identifies a single certificate.
    // The subject is more flexible: any certificate signed by an authority that 
    // you trust with the expected subject can be used. This facilitates 
    // rotation of certificates, but depends on strong public key infrastructure.
    // Depending on how you are distributing client certificates to your clients
    // and your security requirements, either approach can work.
    //
    // In this sample, we are obtaining that information in an unrealistic way.
    // We simply load the certificate file that is also used by the client, and
    // then take the thumbprint or subject from that. In a real deployment, the 
    // certificate should be controlled by the client and not be shared in this 
    // way. We are doing this because we don't know the thumbprint or subject of
    // the certificate that mkcert will generate.
    private static X509Certificate2 ClientCert() =>
        new X509Certificate2("../localhost-client.p12", "changeit");
    private static string ClientCertificateThumbprint() => ClientCert().Thumbprint;
    private static string ClientCertificateSubject() => ClientCert().Subject;

    public static IEnumerable<Client> List =>
        new[]
        {
            new Client
            {
                ClientId = "mtls",

                ClientSecrets =
                {
                    // (Either secret type can be used)
                    // new Secret(ClientCertificateThumbprint())
                    // {
                    //     Type = IdentityServerConstants.SecretTypes.X509CertificateThumbprint
                    // },

                    new Secret(ClientCertificateSubject())
                    {
                        Type = IdentityServerConstants.SecretTypes.X509CertificateName
                    }
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
