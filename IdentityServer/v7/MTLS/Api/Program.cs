// Copyright (c) Duende Software. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.Security.Cryptography.X509Certificates;
using Api;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Serilog;
using Serilog.Sinks.SystemConsole.Themes;

Console.Title = "API";

Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Information()
    .Enrich.FromLogContext()
    .WriteTo.Console(outputTemplate: "[{Timestamp:HH:mm:ss} {Level}] {SourceContext}{NewLine}{Message:lj}{NewLine}{Exception}{NewLine}", theme: AnsiConsoleTheme.Code)
    .CreateLogger();

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSerilog();

builder.Services.AddControllers();

// this API will accept any access token from the authority
builder.Services.AddAuthentication("token")
    .AddJwtBearer("token", options =>
    {
        options.Authority = "https://localhost:5001";
        options.TokenValidationParameters.ValidateAudience = false;
        options.MapInboundClaims = false;

        options.TokenValidationParameters.ValidTypes = new[] { "at+jwt" };
    });

// for local testing, we will use kestrel's MTLS
// this requires DNS to be setup -- hosts file would contain:
// 127.0.0.1 ::1 api.localhost
var mtls_localhost = new X509Certificate2("api.localhost.pfx", "password");
builder.Services.Configure<KestrelServerOptions>(options =>
{
    options.ListenLocalhost(6001, config => config.UseHttps());
    options.ListenLocalhost(6002, config =>
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

app.UseRouting();

app.UseAuthentication();
app.UseConfirmationValidation();

app.UseAuthorization();

app.MapControllers().RequireAuthorization();

app.Run();
