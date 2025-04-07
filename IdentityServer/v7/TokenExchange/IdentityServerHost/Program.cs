// Copyright (c) Duende Software. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.


using IdentityServerHost;
using Serilog;
using Serilog.Sinks.SystemConsole.Themes;

Console.Title = "IdentityServer";

Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Information()
            .Enrich.FromLogContext()
            .WriteTo.Console(outputTemplate: "[{Timestamp:HH:mm:ss} {Level}] {SourceContext}{NewLine}{Message:lj}{NewLine}{Exception}{NewLine}", theme: AnsiConsoleTheme.Code)
            .CreateLogger();

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSerilog();

var idsvrBuilder = builder.Services.AddIdentityServer()
    .AddInMemoryApiScopes(Config.Scopes)
    .AddInMemoryClients(Config.Clients);

// registers extension grant validator for the token exchange grant type
idsvrBuilder.AddExtensionGrantValidator<TokenExchangeGrantValidator>();

// register a profile service to emit the act claim
idsvrBuilder.AddProfileService<ProfileService>();

var app = builder.Build();

app.UseDeveloperExceptionPage();

app.UseIdentityServer();

app.Run();
