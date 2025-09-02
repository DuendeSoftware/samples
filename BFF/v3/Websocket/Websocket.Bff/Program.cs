using Duende.Bff;
using Duende.Bff.Yarp;
using Yarp.ReverseProxy.Configuration;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddBff()
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
    .AddRemoteApis()
    .AddYarpConfig([
        new RouteConfig()
        {
            RouteId = "graphql-route",
            ClusterId = "graphql-cluster",
            Match = new RouteMatch
            {
                // Matches requests to "http://yarp-url/graphql/*"
                Path = "/g1/{**catch-all}"
            },
            // Strips the "/graphql" prefix before forwarding
            Transforms = new[] { new Dictionary<string, string> { { "PathPattern", "{**catch-all}" } } }
        }],
        [new ClusterConfig()
        {
            ClusterId = "graphql-cluster",
            Destinations = new Dictionary<string, DestinationConfig>(StringComparer.OrdinalIgnoreCase)
            {
                {
                    "destination1", new DestinationConfig()
                    { 
                        // Address of your Hot Chocolate server
                        Address = "http://localhost:5095/graphql"
                    }
                }
            }
        }]
    );



// Add services to the container.
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

var summaries = new[]
{
    "Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"
};

app.UseWebSockets();

app.MapReverseProxy();

app.MapGet("/", () => "welcome");

app.MapRemoteBffApiEndpoint("/graphql", new Uri("http://localhost:5095/graphql"));

app.Run();

record WeatherForecast(DateOnly Date, int TemperatureC, string? Summary)
{
    public int TemperatureF => 32 + (int)(TemperatureC / 0.5556);
}
