using OpenApi.Bff;
using Duende.Bff.Yarp;
using Swashbuckle.AspNetCore.SwaggerUI;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

builder.Services.AddBff()
    .AddRemoteApis();

builder.Services.AddHttpForwarderWithServiceDiscovery();



Configuration config = new();
builder.Configuration.Bind("BFF", config);

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
        options.Authority = config.Authority;
        options.ClientId = config.ClientId;
        options.ClientSecret = config.ClientSecret;

        options.ResponseType = "code";
        options.ResponseMode = "query";

        options.GetClaimsFromUserInfoEndpoint = true;
        options.MapInboundClaims = false;
        options.SaveTokens = true;

        options.Scope.Clear();
        foreach (var scope in config.Scopes)
        {
            options.Scope.Add(scope);
        }

        options.TokenValidationParameters = new()
        {
            NameClaimType = "name",
            RoleClaimType = "role"
        };
    });


var app = builder.Build();

app.UseDefaultFiles();
app.UseStaticFiles();

app.Use(async (c, n) =>
{
    c.Request.Headers.Add("X-CSRF", "1");
    await n();
});

app.UseAuthentication();
app.UseBff();

app.MapBffManagementEndpoints();

app.MapRemoteBffApiEndpoint("/api1", Services.Api1.LogicalUri().ToString());
app.MapRemoteBffApiEndpoint("/api2", Services.Api2.LogicalUri().ToString());
//    .RequireAccessToken(api.RequiredToken);
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("../api1/openapi/v1.json", "Api1");
    c.SwaggerEndpoint("../api2/openapi/v1.json", "Api2");
});

app.Run();
