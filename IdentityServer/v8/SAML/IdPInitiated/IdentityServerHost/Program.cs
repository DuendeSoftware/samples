// Copyright (c) Duende Software. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

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

// Load SP signing certificates from environment variables
var sp1CertBase64 = builder.Configuration["Sp1SigningCertificate"];
X509Certificate2? sp1SigningCert = !string.IsNullOrEmpty(sp1CertBase64)
    ? X509CertificateLoader.LoadCertificate(Convert.FromBase64String(sp1CertBase64))
    : null;

var sp2CertBase64 = builder.Configuration["Sp2SigningCertificate"];
X509Certificate2? sp2SigningCert = !string.IsNullOrEmpty(sp2CertBase64)
    ? X509CertificateLoader.LoadCertificate(Convert.FromBase64String(sp2CertBase64))
    : null;

idsvrBuilder.AddInMemorySamlServiceProviders(
[
    new SamlServiceProvider
    {
        EntityId = "https://localhost:5002",
        DisplayName = "HR Portal",
        AllowIdpInitiated = true,
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
        RequireSignedAuthnRequests = sp1SigningCert != null,
        Certificates = sp1SigningCert != null
            ?
            [
                new ServiceProviderCertificate
                {
                    Certificate = sp1SigningCert,
                    Use = KeyUse.Signing
                }
            ]
            : [],
        AllowedScopes = { "openid", "profile" }
    },
    new SamlServiceProvider
    {
        EntityId = "https://localhost:5003",
        DisplayName = "Expense Tracker",
        AllowIdpInitiated = true,
        AssertionConsumerServiceUrls =
        [
            new IndexedEndpoint
            {
                Location = "https://localhost:5003/Saml2/Acs",
                Binding = SamlBinding.HttpPost,
                Index = 0,
                IsDefault = true
            }
        ],
        SingleLogoutServiceUrls =
        [
            new SamlEndpointType
            {
                Location = "https://localhost:5003/Saml2/Logout",
                Binding = SamlBinding.HttpRedirect
            }
        ],
        RequireSignedAuthnRequests = sp2SigningCert != null,
        Certificates = sp2SigningCert != null
            ?
            [
                new ServiceProviderCertificate
                {
                    Certificate = sp2SigningCert,
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
