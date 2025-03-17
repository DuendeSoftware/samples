using Duende.Bff.Yarp;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();

builder.Services.AddBff();

var yarpBuilder = builder.Services.AddReverseProxy()
    .AddTransforms<AccessTokenTransformProvider>();
//Configure from included extension method
yarpBuilder.Configure();

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
app.UseBff();
app.UseAuthorization();

app.MapBffManagementEndpoints();

// if you want the TODOs API local
// endpoints.MapControllers()
//     .RequireAuthorization()
//     .AsBffApiEndpoint();

// if you want the TODOs API remote
app.MapBffReverseProxy();

// which is equivalent to
//endpoints.MapReverseProxy()
//    .AsBffApiEndpoint();

app.Run();