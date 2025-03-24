// Copyright (c) Duende Software. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.


using Duende.IdentityServer;
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

builder.Services.AddRazorPages();

var idsvrBuilder = builder.Services.AddIdentityServer(options =>
{
    options.Events.RaiseErrorEvents = true;
    options.Events.RaiseInformationEvents = true;
    options.Events.RaiseFailureEvents = true;
    options.Events.RaiseSuccessEvents = true;

    // see https://docs.duendesoftware.com/identityserver/v5/basics/resources
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
