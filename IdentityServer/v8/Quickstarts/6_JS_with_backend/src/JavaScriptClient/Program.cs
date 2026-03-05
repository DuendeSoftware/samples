// Copyright (c) Duende Software. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Duende.Bff;
using Duende.Bff.AccessTokenManagement;
using Duende.Bff.Yarp;
using Microsoft.AspNetCore.Authorization;

var builder = WebApplication.CreateBuilder(args);

JwtSecurityTokenHandler.DefaultMapInboundClaims = false;
builder.Services.AddAuthorization();

builder.Services
    .AddBff()
    .ConfigureOpenIdConnect(options =>
    {
        options.Authority = "https://localhost:5001";
        options.ClientId = "bff";
        options.ClientSecret = "secret";
        options.ResponseType = "code";
        options.Scope.Add("api1");
        options.Scope.Add("offline_access");
        options.SaveTokens = true;
        options.GetClaimsFromUserInfoEndpoint = true;
        options.MapInboundClaims = false;
    })
    .ConfigureCookies(options => options.Cookie.SameSite = SameSiteMode.Strict)
    .AddRemoteApis();

var app = builder.Build();
if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
}

app.UseDefaultFiles();
app.UseStaticFiles();

app.UseRouting();
app.UseAuthentication();

app.UseBff();

app.UseAuthorization();

app.MapBffManagementEndpoints();

// Uncomment this for Controller support
// app.MapControllers()
//     .AsBffApiEndpoint();

app.MapGet("/local/identity", LocalIdentityHandler)
    .AsBffApiEndpoint();

app.MapRemoteBffApiEndpoint("/remote", new Uri("https://localhost:6001"))
    .WithAccessToken(RequiredTokenType.User);

app.Run();

[Authorize]
static IResult LocalIdentityHandler(ClaimsPrincipal user, HttpContext context)
{
    var name = user.FindFirst("name")?.Value ?? user.FindFirst("sub")?.Value;
    return Results.Json(new { message = "Local API Success!", user = name });
}
