// Copyright (c) Duende Software. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using IdentityServerHost;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();
builder.Services.AddControllersWithViews();

var idsvrBuilder = builder.Services.AddIdentityServer(options =>
{
    options.UserInteraction.LoginUrl = "/login.html";
    options.UserInteraction.ConsentUrl = "/consent.html";
    options.UserInteraction.LogoutUrl = "/logout.html";
    options.UserInteraction.ErrorUrl = "/error.html";

    // see https://docs.duendesoftware.com/identityserver/fundamentals/resources
    options.EmitStaticAudienceClaim = true;
})
    .AddTestUsers(TestUsers.Users);

// in-memory, code config
idsvrBuilder.AddInMemoryIdentityResources(Config.IdentityResources);
idsvrBuilder.AddInMemoryClients(Config.Clients);

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
}

app.UseDefaultFiles();
app.UseStaticFiles();

app.UseRouting();
app.UseIdentityServer();
app.MapDefaultEndpoints();
app.UseAuthorization();
app.MapDefaultControllerRoute();

app.Run();
