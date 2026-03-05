using Aspire.Web;
using Duende.AccessTokenManagement.OpenIdConnect;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;

var builder = WebApplication.CreateBuilder(args);

// Add service defaults & Aspire components.
builder.AddServiceDefaults();

builder.Services.AddAuthentication(opt =>
{
    opt.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
    opt.DefaultChallengeScheme = OpenIdConnectDefaults.AuthenticationScheme;
})
    .AddCookie()
    .AddOpenIdConnect(opt =>
    {
        opt.Authority = builder.Configuration["services:identityserver:https:0"];
        opt.ClientId = "web";
        opt.ClientSecret = "49C1A7E1-0C79-4A89-A3D6-A37998FB86B0";

        opt.ResponseType = "code";
        opt.Scope.Add("weather");
        opt.Scope.Add("offline_access");

        opt.SaveTokens = true;

        opt.GetClaimsFromUserInfoEndpoint = true;
        opt.MapInboundClaims = false;
    });

builder.Services.AddOpenIdConnectAccessTokenManagement();

// Add services to the container.
builder.Services.AddRazorPages();

builder.Services.AddHttpClient<WeatherApiClient>(client
    => client.BaseAddress = new("https://apiservice"))
    .AddUserAccessTokenHandler();

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
}

app.UseStaticFiles();

app.UseAuthentication();
app.UseAuthorization();

app.MapRazorPages()
    .RequireAuthorization();

app.MapDefaultEndpoints();

app.Run();
