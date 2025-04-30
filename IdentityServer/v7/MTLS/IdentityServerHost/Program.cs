// Copyright (c) Duende Software. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.


using System.Security.Cryptography.X509Certificates;
using IdentityServerHost;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Serilog;
using Serilog.Events;
using Serilog.Sinks.SystemConsole.Themes;

Console.Title = "IdentityServer";

Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Debug()
    .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
    .MinimumLevel.Override("Microsoft.Hosting.Lifetime", LogEventLevel.Information)
    .MinimumLevel.Override("System", LogEventLevel.Warning)
    .MinimumLevel.Override("Microsoft.AspNetCore.Authentication", LogEventLevel.Information)
    .Enrich.FromLogContext()
    .WriteTo.Console(outputTemplate: "[{Timestamp:HH:mm:ss} {Level}] {SourceContext}{NewLine}{Message:lj}{NewLine}{Exception}{NewLine}", theme: AnsiConsoleTheme.Code)
    .CreateLogger();

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSerilog();

builder.Services.AddRazorPages();

builder.Services.AddAuthentication()
    .AddCertificate(opt =>
    {
        // Revocation check disabled for mkcert certificate. 
        // In production, revocation should be checked.
        opt.RevocationMode = X509RevocationMode.NoCheck;
    });

var idsvrBuilder = builder.Services.AddIdentityServer(options =>
{
    options.Events.RaiseErrorEvents = true;
    options.Events.RaiseInformationEvents = true;
    options.Events.RaiseFailureEvents = true;
    options.Events.RaiseSuccessEvents = true;

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
