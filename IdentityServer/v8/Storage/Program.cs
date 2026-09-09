// Copyright (c) Duende Software. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.Security.Cryptography;
using System.Text;
using Duende.IdentityModel;
using Duende.IdentityServer;
using Duende.IdentityServer.Admin;
using Duende.IdentityServer.Admin.Clients;
using Duende.IdentityServer.Services;
using Duende.IdentityServer.Stores;
using Duende.Storage.Querying;
using Duende.Storage.Schema;
using Duende.Storage.Sqlite;
using Microsoft.AspNetCore.HostFiltering;
using Microsoft.AspNetCore.WebUtilities;
using Storage;

const string defaultBaseUrl = "https://localhost:5006";
const string databaseName = "IdentityServerStorageSample";

var builder = WebApplication.CreateBuilder(args);
var publicBaseUri = GetPublicBaseUri(builder.Configuration["SampleBaseUrl"] ?? defaultBaseUrl);
var publicBaseUrl = publicBaseUri.GetLeftPart(UriPartial.Authority);

builder.Services.AddHttpClient();
builder.Services.AddHttpContextAccessor();
builder.Services.AddRazorPages();
builder.Services
    .AddAuthorizationBuilder()
    .AddPolicy("admin", policy => policy.RequireClaim(JwtClaimTypes.Role, "admin"));

builder.Services.Configure<HostFilteringOptions>(options => options.AllowedHosts = [publicBaseUri.Host]);
builder.Services.AddScoped<IRequestRegionResolver, SimulatedRequestRegionResolver>();

builder.Services
    .AddIdentityServer(options =>
    {
        options.KeyManagement.Enabled = false;
        options.ServerSideSessions.UserDisplayNameClaimType = JwtClaimTypes.Name;
    })
    .AddDeveloperSigningCredential(persistKey: false)
    .AddServerSideSessions()
    .AddStorage(storage => storage.AddSqliteInMemoryStore(databaseName))
    .AddInMemoryDataExtensionSchemas([SampleData.ClientSchema])
    .AddTestUsers(SampleUsers.Users)
    .AddCustomTokenRequestValidator<AllowedRegionTokenRequestValidator>();

var app = builder.Build();

await app.Services.GetRequiredService<IDatabaseSchema>().MigrateAsync(CancellationToken.None);
await SampleData.InitializeAsync(app.Services, publicBaseUrl, CancellationToken.None);

app.UseHostFiltering();
app.UseRouting();
app.UseIdentityServer();
app.UseAuthorization();

app.MapGet("/", () => Results.Content(
    content: SamplePage.Create(publicBaseUrl),
    contentType: "text/html")
);

app.MapGet("/sample/clients", async (
    string? search,
    IClientAdmin clientAdmin,
    CancellationToken ct) =>
{
    var request = QueryRequest.Create<ClientFilter, ClientSortField>(
        filter: new ClientFilter { ClientId = search }
    );

    var result = await clientAdmin.QueryAsync(request, ct);

    return Results.Ok(new
    {
        result.Items,
        result.TotalCount,
        result.HasMoreData,
        result.NextToken
    });

}).RequireAuthorization("admin");

app.MapGet("/sample/sessions", async (
    IServerSideSessionStore sessionStore,
    CancellationToken ct) =>
{
    var result = await sessionStore.QuerySessionsAsync(ct);
    return Results.Ok(new
    {
        Sessions = result.Results.Select(session => new
        {
            session.SubjectId,
            session.DisplayName,
            session.Created,
            session.Renewed,
            session.Expires
        }),
        result.Results.Count,
        result.HasNextResults
    });

}).RequireAuthorization("admin");

app.MapGet("/home/error", async (
    string? errorId,
    IIdentityServerInteractionService interaction,
    CancellationToken ct) =>
{
    var error = await interaction.GetErrorContextAsync(errorId, ct);
    return Results.Problem(
        statusCode: StatusCodes.Status400BadRequest,
        title: error?.Error ?? "IdentityServer request error",
        detail: error?.ErrorDescription);

}).AllowAnonymous();

app.MapGet("/sample/login", (HttpContext httpContext) =>
{
    var state = WebEncoders.Base64UrlEncode(RandomNumberGenerator.GetBytes(32));
    var verifier = WebEncoders.Base64UrlEncode(RandomNumberGenerator.GetBytes(32));
    var challenge = WebEncoders.Base64UrlEncode(SHA256.HashData(Encoding.ASCII.GetBytes(verifier)));
    var cookieOptions = new CookieOptions
    {
        HttpOnly = true,
        IsEssential = true,
        MaxAge = TimeSpan.FromMinutes(5),
        SameSite = SameSiteMode.Lax,
        Secure = true
    };

    httpContext.Response.Cookies.Append(SamplePage.StateCookie, state, cookieOptions);
    httpContext.Response.Cookies.Append(SamplePage.VerifierCookie, verifier, cookieOptions);

    var authorizeUrl = QueryHelpers.AddQueryString(
        uri: "/connect/authorize",
        queryString: new Dictionary<string, string?>
        {
            ["client_id"] = SampleData.InteractiveClientId,
            ["redirect_uri"] = $"{publicBaseUrl}/sample/callback",
            ["response_type"] = "code",
            ["scope"] = "openid profile sample-api",
            ["code_challenge"] = challenge,
            ["code_challenge_method"] = "S256",
            ["state"] = state
        });

    return Results.Redirect(authorizeUrl);
});

app.MapGet("/sample/callback", async (
    HttpContext httpContext,
    IHttpClientFactory httpClientFactory,
    CancellationToken ct) =>
{
    var returnedState = httpContext.Request.Query["state"].ToString();
    var expectedState = httpContext.Request.Cookies[SamplePage.StateCookie];
    var verifier = httpContext.Request.Cookies[SamplePage.VerifierCookie];

    httpContext.Response.Cookies.Delete(SamplePage.StateCookie);
    httpContext.Response.Cookies.Delete(SamplePage.VerifierCookie);

    if (string.IsNullOrEmpty(returnedState) ||
        string.IsNullOrEmpty(expectedState) ||
        !CryptographicOperations.FixedTimeEquals(
            Encoding.UTF8.GetBytes(returnedState),
            Encoding.UTF8.GetBytes(expectedState)) ||
        string.IsNullOrEmpty(verifier))
    {
        return Results.BadRequest("The authorization response state or PKCE verifier is invalid.");
    }

    var code = httpContext.Request.Query["code"].ToString();
    if (string.IsNullOrEmpty(code))
    {
        return Results.BadRequest("The authorization response did not contain a code.");
    }

    using var tokenRequest = new HttpRequestMessage(
        method: HttpMethod.Post,
        requestUri: $"{publicBaseUrl}/connect/token")
    {
        Content = new FormUrlEncodedContent(
        [
            new("grant_type", "authorization_code"),
            new("client_id", SampleData.InteractiveClientId),
            new("code", code),
            new("redirect_uri", $"{publicBaseUrl}/sample/callback"),
            new("code_verifier", verifier)
        ])
    };

    tokenRequest.Headers.Add(SampleData.RegionHeaderName, SampleData.AllowedRegion);

    var client = httpClientFactory.CreateClient();
    using var response = await client.SendAsync(tokenRequest, ct);
    var content = await response.Content.ReadAsStringAsync(ct);

    httpContext.Response.Headers.CacheControl = "no-store";
    httpContext.Response.Headers.Pragma = "no-cache";

    return Results.Content(
        content,
        contentType: response.Content.Headers.ContentType?.ToString() ?? "application/json",
        statusCode: (int)response.StatusCode);
});

app.MapRazorPages();

await app.RunAsync();

static Uri GetPublicBaseUri(string value)
{
    if (!Uri.TryCreate(value, UriKind.Absolute, out var uri) ||
        uri.Scheme != Uri.UriSchemeHttps ||
        !uri.IsLoopback)
    {
        throw new InvalidOperationException("SampleBaseUrl must be an absolute HTTPS loopback URL.");
    }

    return uri;
}
