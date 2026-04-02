// Copyright (c) Duende Software. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.


using Duende.IdentityServer;
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

    options.ServerSideSessions.UserDisplayNameClaimType = "name"; // this sets the "name" claim as the display name in the admin tool
    options.ServerSideSessions.RemoveExpiredSessions = true; // removes expired sessions. defaults to true.
    options.ServerSideSessions.ExpiredSessionsTriggerBackchannelLogout = true; // this triggers notification to clients. defaults to false.
})
    .AddTestUsers(TestUsers.Users)
    // enables server-side sessions
    .AddServerSideSessions();

idsvrBuilder.AddInMemoryIdentityResources(Resources.Identity);
idsvrBuilder.AddInMemoryApiScopes(Resources.ApiScopes);
idsvrBuilder.AddInMemoryApiResources(Resources.ApiResources);
idsvrBuilder.AddInMemoryClients(Clients.List);

// this is only needed for the JAR and JWT samples and adds supports for JWT-based client authentication
idsvrBuilder.AddJwtBearerClientAuthentication();

builder.Services.AddAuthentication()
    .AddOpenIdConnect("Google", "Sign-in with Google", options =>
    {
        options.SignInScheme = IdentityServerConstants.ExternalCookieAuthenticationScheme;
        options.ForwardSignOut = IdentityServerConstants.DefaultCookieAuthenticationScheme;

        options.Authority = "https://accounts.google.com/";
        options.ClientId = "708778530804-rhu8gc4kged3he14tbmonhmhe7a43hlp.apps.googleusercontent.com";

        options.CallbackPath = "/signin-google";
        options.Scope.Add("email");
        options.DisableTelemetry = true;
    });

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
