// Copyright (c) Duende Software. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.


using IdentityServerHost;
using Microsoft.AspNetCore.DataProtection;

Console.Title = "IdentityServer";

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

var idsvrBuilder = builder.Services.AddIdentityServer()
    .AddInMemoryApiScopes(Config.Scopes)
    .AddInMemoryClients(Config.Clients);

// registers extension grant validator for the token exchange grant type
idsvrBuilder.AddExtensionGrantValidator<TokenExchangeGrantValidator>();

// register a profile service to emit the act claim
idsvrBuilder.AddProfileService<ProfileService>();

// Add `.PersistKeysTo…()` and `.ProtectKeysWith…()` calls
// See more at https://docs.duendesoftware.com/general/data-protection
builder.Services.AddDataProtection()
    .SetApplicationName("IdentityServer");

var app = builder.Build();

app.MapDefaultEndpoints();

app.UseDeveloperExceptionPage();

app.UseIdentityServer();

app.Run();
