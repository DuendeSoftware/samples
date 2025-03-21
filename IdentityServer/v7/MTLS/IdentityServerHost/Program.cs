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
    // uncomment to write to Azure diagnostics stream
    //.WriteTo.File(
    //    @"D:\home\LogFiles\Application\identityserver.txt",
    //    fileSizeLimitBytes: 1_000_000,
    //    rollOnFileSizeLimit: true,
    //    shared: true,
    //    flushToDiskInterval: TimeSpan.FromSeconds(1))
    .WriteTo.Console(outputTemplate: "[{Timestamp:HH:mm:ss} {Level}] {SourceContext}{NewLine}{Message:lj}{NewLine}{Exception}{NewLine}", theme: AnsiConsoleTheme.Code)
    .CreateLogger();

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSerilog();

builder.Services.AddRazorPages();

var idsvrBuilder = builder.Services.AddIdentityServer(options =>
{
    options.Events.RaiseErrorEvents = true;
    options.Events.RaiseInformationEvents = true;
    options.Events.RaiseFailureEvents = true;
    options.Events.RaiseSuccessEvents = true;

    // see https://docs.duendesoftware.com/identityserver/v5/basics/resources
    options.EmitStaticAudienceClaim = true;

    // MTLS stuff
    options.MutualTls.Enabled = true;
    options.MutualTls.AlwaysEmitConfirmationClaim = true;
    options.MutualTls.DomainName = "mtls.localhost:5099";
    // set this to be explicit when using a domain name for mTLS
    options.IssuerUri = "https://localhost:5001";
});

idsvrBuilder.AddTestUsers(TestUsers.Users);
idsvrBuilder.AddInMemoryClients(Clients.List);
idsvrBuilder.AddInMemoryIdentityResources(Resources.Identity);
idsvrBuilder.AddInMemoryApiScopes(Resources.ApiScopes);

// this allows MTLS to be used as client authentication
idsvrBuilder.AddMutualTlsSecretValidators();

// for local testing, we will use kestrel's MTLS
// this requires DNS to be setup -- hosts file would contain:
// 127.0.0.1 ::1 mtls.localhost
var mtls_localhost = new X509Certificate2("mtls.localhost.pfx", "password");
builder.Services.Configure<KestrelServerOptions>(options =>
{
    options.ListenLocalhost(5001, config => config.UseHttps());
    options.ListenLocalhost(5099, config =>
    {
        config.UseHttps(https =>
        {
            https.ClientCertificateMode = Microsoft.AspNetCore.Server.Kestrel.Https.ClientCertificateMode.RequireCertificate;
            https.AllowAnyClientCertificate();
            https.ServerCertificate = mtls_localhost;
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
