// Copyright (c) Duende Software. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;

var builder = DistributedApplication.CreateBuilder(args);

// Generate a self-signed certificate for the SP to sign SAML logout requests.
// The IdP needs the public key to validate those signatures.
var (pfxBase64, certBase64, password) = GenerateSpSigningCertificate();

var idp = builder.AddProject<Projects.IdentityServerHost>("identityserverhost")
    .WithEnvironment("SpSigningCertificate", certBase64);

builder.AddProject<Projects.ServiceProvider>("serviceprovider")
    .WithEnvironment("SpSigningCertificatePfx", pfxBase64)
    .WithEnvironment("SpSigningCertificatePassword", password)
    .WaitFor(idp);

builder.Build().Run();

static (string PfxBase64, string CertBase64, string Password) GenerateSpSigningCertificate()
{
    const string password = "dev-only"; // not a secret — ephemeral dev cert only

    using var rsa = RSA.Create(2048);
    var request = new CertificateRequest(
        "CN=SAML SP Signing (Dev)",
        rsa,
        HashAlgorithmName.SHA256,
        RSASignaturePadding.Pkcs1);

    request.CertificateExtensions.Add(
        new X509KeyUsageExtension(X509KeyUsageFlags.DigitalSignature, critical: true));

    var cert = request.CreateSelfSigned(
        DateTimeOffset.UtcNow.AddMinutes(-5),
        DateTimeOffset.UtcNow.AddYears(2));

    var pfxBytes = cert.Export(X509ContentType.Pfx, password);
    var certBytes = cert.Export(X509ContentType.Cert);

    return (
        Convert.ToBase64String(pfxBytes),
        Convert.ToBase64String(certBytes),
        password);
}
