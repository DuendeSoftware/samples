using System.Collections.Concurrent;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using HotChocolate.AspNetCore;
using HotChocolate.AspNetCore.Subscriptions;
using HotChocolate.AspNetCore.Subscriptions.Protocols;
using HotChocolate.AspNetCore.Subscriptions.Protocols.Apollo;
using HotChocolate.Subscriptions;
using HotChocolate.Types.Pagination;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Microsoft.IdentityModel.Tokens;
using WebSocket.GraphQLServer.Types;

var builder = WebApplication.CreateBuilder(args);

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



public class Subscription
{
    [Subscribe]
    public Book BookAdded([EventMessage] Book book) => book;

}

public class Mutation
{
    public async Task<Book> AddBook(Book book, [Service] ITopicEventSender sender)
    {
        await sender.SendAsync("BookAdded", book);

        // Omitted code for brevity
        return book;
    }


}
public class JwtBearerValidator(IOptionsFactory<JwtBearerOptions> jwtBearerOptions)
{
    private readonly JwtBearerOptions _jwtOptions = jwtBearerOptions.Create(JwtBearerDefaults.AuthenticationScheme);

    // Inject the validator and the configured options
    // Retrieve the TokenValidationParameters from the options
    // We use the scheme name "Bearer" to get the correct options.

    public async Task<(ClaimsPrincipal Principal, SecurityToken Token)?> ValidateToken(string token)
    {
        // The token often comes with "Bearer " prefix, which needs to be removed.
        var tokenToValidate = token.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase)
            ? token.Substring(7)
            : token;

        var openIdConfig = await _jwtOptions.ConfigurationManager.GetConfigurationAsync(CancellationToken.None);
        

        try
        {
            var tokenValidator = new JwtSecurityTokenHandler();
            // Validate the token using the injected validator and parameters.
            // This method throws an exception if the token is invalid.
            // 2. Clone the TokenValidationParameters from your options.
            var validationParameters = _jwtOptions.TokenValidationParameters.Clone();

            // 3. Apply the discovered issuer and signing keys.
            validationParameters.ValidIssuer = openIdConfig.Issuer;
            validationParameters.IssuerSigningKeys = openIdConfig.SigningKeys;

            // 4. Validate the token using the fully populated parameters.
            var claimsPrincipal = tokenValidator.ValidateToken(
                tokenToValidate,
                validationParameters,
                out SecurityToken validatedToken);

            return (claimsPrincipal, validatedToken);
        }
        catch (SecurityTokenException ex)
        {
            // Token validation failed (e.g., expired, invalid signature, etc.)
            Console.WriteLine($"Token validation failed: {ex.Message}");
            return null;
        }
        catch (Exception ex)
        {
            // Some other error occurred
            Console.WriteLine($"An error occurred: {ex.Message}");
            return null;
        }
    }
}

public class SubscriptionAuthMiddleware(JwtBearerValidator bearerValidator) : DefaultSocketSessionInterceptor
{
    private ConcurrentDictionary<ISocketSession, Connection> _connections = new();

    public override async ValueTask<ConnectionStatus> OnConnectAsync(ISocketSession session, IOperationMessagePayload message, CancellationToken cancellationToken)
    {
        try
        {
            var httpContext = session.Connection.HttpContext;
            var bearerToken = httpContext.Request.Headers.Authorization;
            if (string.IsNullOrEmpty(bearerToken))
            {
                return ConnectionStatus.Accept();
            }

            var token = bearerToken.ToString().Replace("Bearer ", string.Empty, StringComparison.OrdinalIgnoreCase);
            var validatedToken = await bearerValidator.ValidateToken(token);

            //if (validatedToken == null)
            //{
            //    return ConnectionStatus.Reject("provided token was invalid");
            //}

            CancellationTokenSource ct = new CancellationTokenSource();

            _connections.TryAdd(session, new Connection()
            {
                TokenSource = ct,
                CancelTask = Task.Delay(TimeSpan.FromSeconds(5))
                    .ContinueWith(async (_, __) =>
                    {
                        _connections.Remove(session, out var _);
                        await session.Connection.CloseAsync("Refresh", ConnectionCloseReason.NormalClosure,
                            ct.Token);
                    }, null, ct.Token)
            });

            return ConnectionStatus.Accept();
        }
        catch (Exception ex)
        {
            return ConnectionStatus.Reject(ex.Message);
        }
    }

    public override async ValueTask OnCloseAsync(ISocketSession session, CancellationToken cancellationToken = new CancellationToken())
    {
        if (_connections.TryRemove(session, out var connection))
        {
            await connection.TokenSource.CancelAsync();
            connection.Dispose();
        }
    }
}

public record Connection : IDisposable
{
    public required Task CancelTask { get; init; }
    public required CancellationTokenSource TokenSource { get; init; }

    public void Dispose()
    {
        CancelTask.Dispose();
        TokenSource.Dispose();
    }
}
