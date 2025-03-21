using OpenApi.Bff;
using Duende.Bff.Yarp;
using Microsoft.AspNetCore.Authentication;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

builder.Services.AddBff()
    .AddRemoteApis();

// Make sure Yarp understands aspire's service discovery. 
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
        options.LoginPath = "/bff/login";
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

builder.Services.AddSingleton<BffYarpTransformBuilder>((path, c) =>
{
    DefaultBffYarpTransformerBuilders.DirectProxyWithAccessToken(path, c);
    c.ResponseTransforms.Add(new OpenApiResponseTransform(path));
});


var app = builder.Build();

app.UseDefaultFiles();
app.UseStaticFiles();

app.UseAuthentication();
app.UseBff();

app.MapBffManagementEndpoints();

// proxy all api's. 
app.MapRemoteBffApiEndpoint("/api1", Services.Api1.LogicalUri().ToString())
    .WithOptionalUserAccessToken();
app.MapRemoteBffApiEndpoint("/api2", Services.Api2.LogicalUri().ToString())
    .WithOptionalUserAccessToken();

app.UseSwaggerUI(c =>
{
    // Inject a javascript function to add a CSRF header to all requests
    c.UseRequestInterceptor("function(request){ request.headers['X-CSRF'] = '1';return request;}");

    // Add some javascript that adds a login / logout button to the page. 
    c.InjectJavascript("bff-auth-button.js");

    // Add all swagger endpoints for all APIs
    c.SwaggerEndpoint("/api1/openapi/v1.json", "Api1");
    c.SwaggerEndpoint("/api2/openapi/v1.json", "Api2");
});

app.Run();
