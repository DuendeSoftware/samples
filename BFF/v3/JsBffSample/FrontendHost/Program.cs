using Duende.Bff;
using Duende.Bff.Yarp;
using Microsoft.Extensions.Options;
using Swashbuckle.AspNetCore.SwaggerUI;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddBff()
    .AddRemoteApis();

// registers HTTP client that uses the managed user access token
builder.Services.AddUserAccessTokenHttpClient("api_client", configureClient: client =>
{
    client.BaseAddress = new Uri("https://localhost:5002/");
});

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
        options.Authority = "https://demo.duendesoftware.com";
        options.ClientId = "interactive.confidential";
        options.ClientSecret = "secret";
        options.ResponseType = "code";
        options.ResponseMode = "query";

        options.GetClaimsFromUserInfoEndpoint = true;
        options.MapInboundClaims = false;
        options.SaveTokens = true;
        options.DisableTelemetry = true;

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
    });

var app = builder.Build();

app.UseDefaultFiles();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthentication();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();

    app.Use(async (context, next) =>
    {
        var swaggerUIOptions = context.RequestServices.GetRequiredService<IOptions<SwaggerUIOptions>>().Value;
        if (context.Request.Headers.Referer.Any(it => it.EndsWith($"/{swaggerUIOptions.RoutePrefix}/index.html")))
        {
            var bffOptions = context.RequestServices.GetRequiredService<IOptions<BffOptions>>().Value;
            context.Request.Headers[bffOptions.AntiForgeryHeaderName]= bffOptions.AntiForgeryHeaderValue;
        }
        await next();
    });
}

app.UseBff();
app.UseAuthorization();

app.MapBffManagementEndpoints();

// if you want the TODOs API local
app.MapControllers()
    .RequireAuthorization()
    .AsBffApiEndpoint();

// if you want the TODOs API remote
// app.MapRemoteBffApiEndpoint("/todos", "https://localhost:5020/todos")
//     .RequireAccessToken(Duende.Bff.TokenType.User);

app.Run();