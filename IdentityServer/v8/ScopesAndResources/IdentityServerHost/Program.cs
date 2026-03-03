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

var idsvrBuilder = builder.Services.AddIdentityServer(options =>
{
    // emits static audience if required
    options.EmitStaticAudienceClaim = false;

    // control format of scope claim
    options.EmitScopesAsSpaceDelimitedStringInJwt = true;
})
    .AddInMemoryApiScopes(Config.Scopes)
    .AddInMemoryApiResources(Config.Resources)
    .AddInMemoryClients(Config.Clients);

// registers the scope parser for the transaction scope
idsvrBuilder.AddScopeParser<ParameterizedScopeParser>();

// register the token request validator to access the parsed scope in the pipeline
idsvrBuilder.AddCustomTokenRequestValidator<TokenRequestValidator>();

var app = builder.Build();

app.UseDeveloperExceptionPage();

app.UseIdentityServer();

app.Run();
