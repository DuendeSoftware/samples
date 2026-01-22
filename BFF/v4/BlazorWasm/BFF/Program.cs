using Duende.Bff;
using Duende.Bff.Yarp;
using Microsoft.AspNetCore.Authentication;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddAuthorization();
builder.Services.AddCascadingAuthenticationState();
builder.Services
    .AddBff()
    .AddRemoteApis()
    .ConfigureOpenIdConnect(options =>
    {
        options.Authority = "https://demo.duendesoftware.com";
    
        options.ClientId = "interactive.confidential";
        options.ClientSecret = "secret";
        options.ResponseType = "code";
        options.ResponseMode = "query";
    
        options.Scope.Clear();
        options.Scope.Add("openid");
        options.Scope.Add("profile");
        options.Scope.Add("api");
        options.Scope.Add("offline_access");

        options.MapInboundClaims = false;
        options.ClaimActions.MapAll();
        options.GetClaimsFromUserInfoEndpoint = true;
        options.SaveTokens = true;

        options.TokenValidationParameters.NameClaimType = "name";
        options.TokenValidationParameters.RoleClaimType = "role";
    })
    .ConfigureCookies(options =>
    {
        options.Cookie.Name = "__Host-blazor";
        options.Cookie.SameSite = SameSiteMode.Strict;
    });

var app = builder.Build();

app.UseAuthentication();
app.UseBff();
app.UseAuthorization();

app.MapBffManagementEndpoints();

app.MapStaticAssets();

app.MapGet("/api/data", async () =>
{
    var json = await File.ReadAllTextAsync("weather.json");
    return Results.Content(json, "application/json");
}).RequireAuthorization().AsBffApiEndpoint();

app.MapRemoteBffApiEndpoint("/remoteapi", new Uri("https://demo.duendesoftware.com/api"))
    .WithAccessToken();

app.MapFallbackToFile("index.html");

app.Run();
