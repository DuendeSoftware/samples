// Copyright (c) Duende Software. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using IdentityServerHost;

var builder = WebApplication.CreateBuilder(args);
builder.AddServiceDefaults();
builder.Services.AddRazorPages();

var idsvrBuilder = builder.Services.AddIdentityServer(options =>
{
})
    .AddTestUsers(TestUsers.Users);

// in-memory, code config
idsvrBuilder.AddInMemoryIdentityResources(Config.IdentityResources);
idsvrBuilder.AddInMemoryApiScopes(Config.ApiScopes);
idsvrBuilder.AddInMemoryApiResources(Config.ApiResources);
idsvrBuilder.AddInMemoryClients(Config.Clients);

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
