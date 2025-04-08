// Copyright (c) Duende Software. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using Duende.Bff.Yarp;
using Microsoft.AspNetCore.Authentication.OpenIdConnect; 
using Microsoft.IdentityModel.Tokens;
using Microsoft.IdentityModel.Logging;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers(); 

builder.Services.AddBff()
    .AddRemoteApis(); 

builder.Services.AddUserAccessTokenHttpClient("api_client", configureClient: client =>
{
    client.BaseAddress = new Uri("https://localhost:5002/");
});
if (builder.Environment.IsDevelopment())
{
    IdentityModelEventSource.ShowPII = true;
}

builder.Services.AddAuthentication(options =>
{
    options.DefaultScheme = "cookie";
    options.DefaultChallengeScheme = "oidc";
    options.DefaultSignOutScheme = "oidc";
})
    .AddCookie("cookie", options =>
    {
        options.Cookie.Name = "__Host-bff";
        options.Cookie.SameSite = SameSiteMode.Strict;
    })
    .AddOpenIdConnect("oidc", options =>
    {
        options.Authority = "https://localhost:7051";
        options.ClientId = "interactive";
        options.ClientSecret = "49C1A7E1-0C79-4A89-A3D6-A37998FB86B0";
        options.ResponseType = "code";
        options.ResponseMode = "query";

        options.SignedOutCallbackPath = "/signout-callback-oidc";

        options.GetClaimsFromUserInfoEndpoint = true;
        options.MapInboundClaims = false;
        options.SaveTokens = true;

        options.Scope.Clear();
        options.Scope.Add("openid");
        options.Scope.Add("profile");
        options.Scope.Add("scope2");
        options.Scope.Add("offline_access");

        options.TokenValidationParameters = new TokenValidationParameters
        {
            NameClaimType = "name",
            RoleClaimType = "role"
        };

       
        options.MetadataAddress = "https://containerizedidentityserver:443/.well-known/openid-configuration";

        
        options.BackchannelHttpHandler = new HttpClientHandler
        {
            ServerCertificateCustomValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator
        };

        options.Events = new OpenIdConnectEvents
        {
            OnRedirectToIdentityProvider = context =>
            {
                context.ProtocolMessage.IssuerAddress =
                    "https://localhost:7051/connect/authorize";
                return Task.CompletedTask;
            },

            OnRedirectToIdentityProviderForSignOut = context =>
            {
                var endSessionUri = new UriBuilder(options.Authority);
                endSessionUri.Path = "/connect/endsession";
                context.ProtocolMessage.IssuerAddress = endSessionUri.ToString();
                return Task.CompletedTask;
            }
        };
        options.DisableTelemetry = true;
    });

var app = builder.Build();

// --- Middleware Pipeline ---
app.UseDefaultFiles();
app.UseStaticFiles();
app.UseRouting();
app.UseHttpsRedirection();
app.UseHsts();
app.UseAuthentication();
app.UseBff();
app.UseAuthorization();

// --- Endpoints ---
app.MapBffManagementEndpoints();
app.MapControllers()
    .RequireAuthorization()
    .AsBffApiEndpoint();

app.Run();
