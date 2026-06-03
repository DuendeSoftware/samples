// Copyright (c) Duende Software. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;

var builder = DistributedApplication.CreateBuilder(args);

// Generate a signing certificate per SP instance.
// Each SP gets its own cert; the IdP gets the public keys for signature validation.
var (sp1PfxBase64, sp1CertBase64, password) = GenerateSpSigningCertificate("SAML SP1 Signing (Dev)");
var (sp2PfxBase64, sp2CertBase64, _) = GenerateSpSigningCertificate("SAML SP2 Signing (Dev)");

var idp = builder.AddProject<Projects.IdentityServerHost>("identityserverhost")
    .WithEnvironment("Sp1SigningCertificate", sp1CertBase64)
    .WithEnvironment("Sp2SigningCertificate", sp2CertBase64);

builder.AddProject<Projects.ServiceProvider>("hr-portal")
    .WithEnvironment("SpEntityId", "https://localhost:5002")
    .WithEnvironment("SpBaseUrl", "https://localhost:5002")
    .WithEnvironment("AppName", "HR Portal")
    .WithEnvironment("SpSigningCertificatePfx", sp1PfxBase64)
    .WithEnvironment("SpSigningCertificatePassword", password) // not a secret — ephemeral dev cert only
    .WithHttpsEndpoint(port: 5002, name: "https", isProxied: false)
    .WaitFor(idp);

builder.AddProject<Projects.ServiceProvider>("expense-tracker")
    .WithEnvironment("SpEntityId", "https://localhost:5003")
    .WithEnvironment("SpBaseUrl", "https://localhost:5003")
    .WithEnvironment("AppName", "Expense Tracker")
    .WithEnvironment("SpSigningCertificatePfx", sp2PfxBase64)
    .WithEnvironment("SpSigningCertificatePassword", password) // not a secret — ephemeral dev cert only
    .WithHttpsEndpoint(port: 5003, name: "https", isProxied: false)
    .WaitFor(idp);

builder.Build().Run();

static (string PfxBase64, string CertBase64, string Password) GenerateSpSigningCertificate(string subjectName)
{
    const string password = "dev-only"; // not a secret — ephemeral dev cert only

    using var rsa = RSA.Create(2048);
    var request = new CertificateRequest(
        $"CN={subjectName}",
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
