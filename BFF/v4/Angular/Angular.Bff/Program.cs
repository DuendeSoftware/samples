// Copyright (c) Duende Software. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using Angular.Bff;
using Duende.Bff;
using Duende.Bff.DynamicFrontends;
using Duende.Bff.Yarp;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddAuthentication(options =>
{
    options.DefaultScheme = BffAuthenticationSchemes.BffCookie;
    options.DefaultChallengeScheme = BffAuthenticationSchemes.BffOpenIdConnect;
    options.DefaultSignOutScheme = BffAuthenticationSchemes.BffOpenIdConnect;
});

builder.Services.AddBff()
    .AddRemoteApis()
    .ConfigureOpenIdConnect(options =>
    {
        options.Authority = "https://demo.duendesoftware.com";
        options.ClientId = "interactive.confidential";
        options.ClientSecret = "secret";
        options.ResponseType = "code";
        options.ResponseMode = "query";

        options.GetClaimsFromUserInfoEndpoint = true;
        options.MapInboundClaims = false;
        options.SaveTokens = true;

        options.Scope.Clear();
        options.Scope.Add("openid");
        options.Scope.Add("profile");
        options.Scope.Add("api");
        options.Scope.Add("offline_access");

        options.TokenValidationParameters = new()
        {
            NameClaimType = "name",
            RoleClaimType = "role"
        };
    })
    .ConfigureCookies(options =>
    {
        options.Cookie.Name = "__Host-bff";
        options.Cookie.SameSite = SameSiteMode.Strict;
    });

builder.Services.AddAuthorization();

var app = builder.Build();

app.UseDefaultFiles();
app.MapStaticAssets();

// Configure the HTTP request pipeline.

app.UseHttpsRedirection();
app.UseAuthentication();
app.UseBff();
app.UseAuthorization();
app.MapBffManagementEndpoints();

// Comment this out to use the external api
app.MapGroup("/todos")
    .ToDoGroup()
    .RequireAuthorization()
    .AsBffApiEndpoint();

// Comment this in to use the external api
//app.MapRemoteBffApiEndpoint("/todos", "https://localhost:7001/todos")
//    .RequireAccessToken(Duende.Bff.TokenType.User);

app.MapFallbackToFile("/index.html");

app.Run();

