// Copyright (c) Duende Software. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using IdentityServerHost;
using Microsoft.AspNetCore.DataProtection;

Console.Title = "IdentityServer";

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

builder.Services.AddRazorPages();

var idsvrBuilder = builder.Services.AddIdentityServer(options =>
{
    // see https://docs.duendesoftware.com/identityserver/fundamentals/resources/
    options.EmitStaticAudienceClaim = true;
})
    .AddTestUsers(TestUsers.Users);

idsvrBuilder.AddInMemoryIdentityResources(Resources.Identity);
idsvrBuilder.AddInMemoryApiScopes(Resources.ApiScopes);
idsvrBuilder.AddInMemoryClients(Clients.List);

// Add `.PersistKeysTo…()` and `.ProtectKeysWith…()` calls
// See more at https://docs.duendesoftware.com/general/data-protection
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
