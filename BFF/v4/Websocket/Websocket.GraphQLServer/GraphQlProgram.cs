using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.HttpOverrides;
using Websocket.GraphQLServer.Types;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSingleton<FakeDatabase>();

builder.AddGraphQL()
    .AddAuthorization()
    .AddInMemorySubscriptions()
    .AddSubscriptionType<Subscription>()
    .AddMutationType<Mutation>()
    .AddMutationConventions()
    .AddTypes()
    .AddSocketSessionInterceptor<SubscriptionAuthMiddleware>()
    ;

builder.Services.AddSingleton<JwtBearerValidator>();

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.Authority = "https://demo.duendesoftware.com";
        options.TokenValidationParameters = new ()
        {
            ValidateAudience = false,
            ValidIssuer = "https://demo.duendesoftware.com",
        };
    });

var app = builder.Build();

app.UseForwardedHeaders(new ForwardedHeadersOptions
{
    ForwardedHeaders = ForwardedHeaders.XForwardedFor |
                       ForwardedHeaders.XForwardedProto |
                       ForwardedHeaders.XForwardedHost
});
app.UseAuthentication();

app.UseWebSockets();
app.MapGraphQL();

app.RunWithGraphQLCommands(args);
app.UseRouting();
