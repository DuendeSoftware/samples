// Copyright (c) Duende Software. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.


using System.Security.Cryptography.X509Certificates;
using IdentityServerHost;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Server.Kestrel.Core;

Console.Title = "IdentityServer";

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

builder.Services.AddRazorPages();

builder.Services.AddAuthentication()
    .AddCertificate(opt =>
    {
        // Revocation check disabled for mkcert certificate.
        // In production, revocation should be checked.
        opt.RevocationMode = X509RevocationMode.NoCheck;
    });

// Add `.PersistKeysTo…()` and `.ProtectKeysWith…()` calls
// See more at https://docs.duendesoftware.com/general/data-protection
builder.Services.AddDataProtection()
    .SetApplicationName("IdentityServer");

var idsvrBuilder = builder.Services.AddIdentityServer(options =>
{
    // MTLS Configuration
    options.MutualTls.Enabled = true;
});

idsvrBuilder.AddTestUsers(TestUsers.Users);
idsvrBuilder.AddInMemoryClients(Clients.List);
idsvrBuilder.AddInMemoryIdentityResources(Resources.Identity);
idsvrBuilder.AddInMemoryApiScopes(Resources.ApiScopes);

// this allows MTLS to be used as client authentication
idsvrBuilder.AddMutualTlsSecretValidators();

// for local testing, we will use kestrel's mTLS
builder.Services.Configure<KestrelServerOptions>(options =>
{
    options.ListenLocalhost(5001, config =>
    {
        config.UseHttps(https =>
        {
            https.ClientCertificateMode = Microsoft.AspNetCore.Server.Kestrel.Https.ClientCertificateMode.AllowCertificate;
            https.AllowAnyClientCertificate();
        });
    });
});

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
