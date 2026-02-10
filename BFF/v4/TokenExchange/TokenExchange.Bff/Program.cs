// Copyright (c) Duende Software. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using Duende.Bff;
using Duende.Bff.Yarp;
using Serilog;
using Serilog.Events;
using Serilog.Sinks.SystemConsole.Themes;
using TokenExchange.Bff;

Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Information()
            .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
            .MinimumLevel.Override("Microsoft.Hosting.Lifetime", LogEventLevel.Information)
            .MinimumLevel.Override("Microsoft.AspNetCore.Authentication", LogEventLevel.Information)
            .MinimumLevel.Override("IdentityModel", LogEventLevel.Debug)
            .MinimumLevel.Override("Duende.Bff", LogEventLevel.Debug)
            .Enrich.FromLogContext()
            .WriteTo.Console(outputTemplate: "[{Timestamp:HH:mm:ss} {Level}] {SourceContext}{NewLine}{Message:lj}{NewLine}{Exception}{NewLine}", theme: AnsiConsoleTheme.Code)
            .CreateLogger();

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSerilog();
// Add BFF services to DI - also add server-side session management
builder.Services.AddBff(options =>
{
    //options.UserEndpointReturnNullForAnonymousUser = true;
})
    .AddRemoteApis()
    .AddServerSideSessions();

// local APIs
builder.Services.AddControllers();

// cookie options
builder.Services.AddAuthentication(options =>
{
    options.DefaultScheme = "cookie";
    options.DefaultChallengeScheme = "oidc";
    options.DefaultSignOutScheme = "oidc";
})
    .AddCookie("cookie", options =>
    {
        // set session lifetime
        options.ExpireTimeSpan = TimeSpan.FromHours(8);

        // sliding or absolute
        options.SlidingExpiration = false;

        // host prefixed cookie name
        options.Cookie.Name = "__Host-bff-token-exchange";

        // strict SameSite handling
        options.Cookie.SameSite = SameSiteMode.Strict;
    })
    .AddOpenIdConnect("oidc", options =>
    {
        options.Authority = "https://localhost:5001";

        // confidential client using code flow + PKCE
        options.ClientId = "spa";
        options.ClientSecret = "secret";
        options.ResponseType = "code";
        options.ResponseMode = "query";

        options.MapInboundClaims = false;
        options.GetClaimsFromUserInfoEndpoint = true;
        options.SaveTokens = true;
        options.DisableTelemetry = true;

        // request scopes + refresh tokens
        options.Scope.Clear();
        options.Scope.Add("openid");
        options.Scope.Add("profile");
        options.Scope.Add("api");
        options.Scope.Add("offline_access");
    });
builder.Services.AddSingleton<ImpersonationAccessTokenRetriever>();

builder.Services.AddUserAccessTokenHttpClient("api",
    configureClient: client =>
    {
        client.BaseAddress = new Uri("https://localhost:7001/api");
    });

var app = builder.Build();

app.UseSerilogRequestLogging();
app.UseDeveloperExceptionPage();

app.UseDefaultFiles();
app.UseStaticFiles();

app.UseAuthentication();
app.UseRouting();

// adds antiforgery protection for local APIs
app.UseBff();

// adds authorization for local and remote API endpoints
app.UseAuthorization();

app.MapControllers()
        .RequireAuthorization()
        .AsBffApiEndpoint();

// login, logout, user, backchannel logout...
app.MapBffManagementEndpoints();

// On this path, we require the user token
app.MapRemoteBffApiEndpoint("/api/user-token", "https://localhost:7001")
    .RequireAccessToken(TokenType.User);

// On this path, we perform token exchange to impersonate a different user
// before making the api request
app.MapRemoteBffApiEndpoint("/api/impersonation", "https://localhost:7001")
    .RequireAccessToken(TokenType.User)
    .WithAccessTokenRetriever<ImpersonationAccessTokenRetriever>();

app.Run();
