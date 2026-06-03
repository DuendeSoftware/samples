// Copyright (c) Duende Software. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.Security.Cryptography.X509Certificates;
using Microsoft.AspNetCore.Authentication.Cookies;
using Sustainsys.Saml2;
using Sustainsys.Saml2.AspNetCore2;
using Sustainsys.Saml2.Metadata;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

builder.Services.AddRazorPages();

// SP configuration is driven by environment variables so the same project
// can be launched multiple times by Aspire with different identities.
var spEntityId = builder.Configuration["SpEntityId"] ?? "https://localhost:5002";
var spBaseUrl = builder.Configuration["SpBaseUrl"] ?? "https://localhost:5002";
var appName = builder.Configuration["AppName"] ?? "SAML Service Provider";

builder.Services.AddAuthentication(options =>
{
    options.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = Saml2Defaults.Scheme;
})
    .AddCookie()
    .AddSaml2(options =>
    {
        options.SPOptions.EntityId = new EntityId(spEntityId);
        options.SPOptions.ReturnUrl = new Uri(spBaseUrl + "/Secure");

        // Load SP signing certificate from environment variable (base64 PFX)
        var spCertPfxBase64 = builder.Configuration["SpSigningCertificatePfx"];
        var spCertPassword = builder.Configuration["SpSigningCertificatePassword"];
        if (!string.IsNullOrEmpty(spCertPfxBase64))
        {
            var pfxBytes = Convert.FromBase64String(spCertPfxBase64);
            var cert = X509CertificateLoader.LoadPkcs12(pfxBytes, spCertPassword);
            options.SPOptions.ServiceCertificates.Add(
                new ServiceCertificate { Certificate = cert, Use = CertificateUse.Signing });
        }

        // Allow IdP-initiated (unsolicited) SAML responses
        options.IdentityProviders.Add(new IdentityProvider(
            new EntityId("https://localhost:5001/Saml2"), options.SPOptions)
        {
            LoadMetadata = true,
            AllowUnsolicitedAuthnResponse = true
        });
    });

builder.Services.AddSingleton(new AppSettings(appName));

var app = builder.Build();

app.MapDefaultEndpoints();

if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
}

app.UseStaticFiles();
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();

app.MapRazorPages();

app.Run();

public sealed record AppSettings(string AppName);
