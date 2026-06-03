// Copyright (c) Duende Software. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

#nullable enable
using System.Security.Cryptography.X509Certificates;
using Duende.IdentityServer.Models;
using Duende.IdentityServer.Saml.Models;
using IdentityServerHost;
using Microsoft.AspNetCore.DataProtection;

Console.Title = "IdentityServer";

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

builder.Services.AddRazorPages();

var idsvrBuilder = builder.Services.AddIdentityServer(options =>
{
    options.EmitStaticAudienceClaim = true;
})
    .AddTestUsers(TestUsers.Users)
    .AddSaml();

idsvrBuilder.AddInMemoryIdentityResources(IdentityServerHost.Resources.Identity);

// Load the SP signing certificate (public key only) from environment variable
var spCertBase64 = builder.Configuration["SpSigningCertificate"];
X509Certificate2? spSigningCert = !string.IsNullOrEmpty(spCertBase64)
    ? X509CertificateLoader.LoadCertificate(Convert.FromBase64String(spCertBase64))
    : null;

idsvrBuilder.AddInMemorySamlServiceProviders(
[
    new SamlServiceProvider
    {
        EntityId = "https://localhost:5002",
        DisplayName = "SAML Sample SP",
        AssertionConsumerServiceUrls =
        [
            new IndexedEndpoint
            {
                Location = "https://localhost:5002/Saml2/Acs",
                Binding = SamlBinding.HttpPost,
                Index = 0,
                IsDefault = true
            }
        ],
        SingleLogoutServiceUrls =
        [
            new SamlEndpointType
            {
                Location = "https://localhost:5002/Saml2/Logout",
                Binding = SamlBinding.HttpRedirect
            }
        ],
        RequireSignedAuthnRequests = spSigningCert != null,
        Certificates = spSigningCert != null
            ?
            [
                new ServiceProviderCertificate
                {
                    Certificate = spSigningCert,
                    Use = KeyUse.Signing
                }
            ]
            : [],
        AllowedScopes = { "openid", "profile" }
    }
]);

builder.Services.AddDataProtection()
    .SetApplicationName("IdentityServer");

var app = builder.Build();

app.MapDefaultEndpoints();

if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
}

app.UseStaticFiles();

app.UseRouting();
app.UseIdentityServer();
app.UseAuthorization();
app.MapRazorPages();

app.Run();
