// Copyright (c) Duende Software. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System;
using System.IdentityModel.Tokens.Jwt;
using Client;
using Duende.AccessTokenManagement;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;

var builder = WebApplication.CreateBuilder(args);

JwtSecurityTokenHandler.DefaultMapInboundClaims = false;

builder.Services.AddControllersWithViews();
builder.Services.AddHttpClient();

builder.Services.AddAuthentication(options =>
{
    options.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = "oidc";
})
    .AddCookie(options =>
    {
        options.Cookie.Name = "mvc";

        options.Events.OnSigningOut = async e =>
        {
            // automatically revoke refresh token at signout time
            await e.HttpContext.RevokeRefreshTokenAsync();
        };
    })
    .AddOpenIdConnect("oidc", options =>
    {
        options.Authority = Urls.IdentityServer;

        // no static client secret
        // the secret id created dynamically
        options.ClientId = builder.Configuration["ClientId"];

        // needed to add JWR / private_key_jwt support
        options.EventsType = typeof(OidcEvents);

        // TODO: PushedAuthorization was added in .NET 9 and is now turned on by default.
        options.PushedAuthorizationBehavior = PushedAuthorizationBehavior.Disable;

        // code flow + PKCE (PKCE is turned on by default)
        options.ResponseType = "code";
        options.UsePkce = true;

        options.Scope.Clear();
        options.Scope.Add("openid");
        options.Scope.Add("profile");
        options.Scope.Add("scope1");
        options.Scope.Add("offline_access");

        // not mapped by default
        options.ClaimActions.MapJsonKey("website", "website");

        // keeps id_token smaller
        options.GetClaimsFromUserInfoEndpoint = true;
        options.SaveTokens = true;
        options.MapInboundClaims = false;
        options.DisableTelemetry = true;

        options.TokenValidationParameters = new TokenValidationParameters
        {
            NameClaimType = "name",
            RoleClaimType = "role"
        };
    });

// add service to create JWTs
builder.Services.AddSingleton<AssertionService>();

// add event handler for OIDC events
builder.Services.AddTransient<OidcEvents>();

// add automatic token management
builder.Services.AddOpenIdConnectAccessTokenManagement();

// add service to create assertions for token management
builder.Services.AddTransient<IClientAssertionService, ClientAssertionService>();

// add HTTP client to call protected API
builder.Services.AddUserAccessTokenHttpClient("client", configureClient: client =>
{
    client.BaseAddress = new Uri(Urls.SampleApi);
});

var app = builder.Build();

app.UseDeveloperExceptionPage();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapDefaultControllerRoute()
    .RequireAuthorization();

app.Run();


