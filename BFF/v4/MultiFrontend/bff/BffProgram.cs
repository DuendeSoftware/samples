using System.Diagnostics;
using System.Net;
using Duende.Bff;
using Duende.Bff.AccessTokenManagement;
using Duende.Bff.DynamicFrontends;
using Duende.Bff.Yarp;
using Microsoft.IdentityModel.JsonWebTokens;
using Yarp.ReverseProxy.Forwarder;

var builder = WebApplication.CreateBuilder(args);
builder.AddServiceDefaults();

builder.Services.AddBff(options => options.DisableAntiForgeryCheck = (c) => true)
    .ConfigureOpenIdConnect(options =>
    {
        options.Authority = "https://demo.duendesoftware.com";
        options.ClientId = "interactive.confidential.short"; // Access tokens are valid for 1 minute 15 seconds
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
    .AddFrontends(
        new BffFrontend(BffFrontendName.Parse("customer-portal"))
            .MappedToPath(LocalPath.Parse("/customers"))
            //.WithIndexHtmlUrl(ServiceDiscovery.ResolveService("customer-portal"))
            //.WithRemoteApis(new RemoteApi(LocalPath.Parse("/"), ServiceDiscovery.ResolveService("customer-portal")).WithAccessToken(RequiredTokenType.None))
            ,
        new BffFrontend(BffFrontendName.Parse("management-app"))
            .MappedToPath(LocalPath.Parse("/management"))
            //.WithIndexHtmlUrl(ServiceDiscovery.ResolveService("management-app"))
            //.WithRemoteApis(new RemoteApi(LocalPath.Parse("/"), ServiceDiscovery.ResolveService("management-app")).WithAccessToken(RequiredTokenType.None))
    );


var app = builder.Build();

app.UseHttpsRedirection();

app.UseDefaultFiles();
app.UseAuthentication();
app.UseRouting();
app.UseBff();

// adds authorization for local and remote API endpoints
//app.UseAuthorization();
app.MapGet("/{*rest}", async (IHttpForwarder forwarder, HttpContext context) =>
{
    await ForwardAllRequestsToNpmDevServer(forwarder, context, "https://localhost:3000");
});

app.MapRemoteBffApiEndpoint("/api", ServiceDiscovery.ResolveService("api"))
    .WithAccessToken(RequiredTokenType.User)
    .SkipAntiforgery();



app.Run();

static async Task ForwardAllRequestsToNpmDevServer(IHttpForwarder forwarder, HttpContext context, string url)
{
    var httpClient = new HttpMessageInvoker(
        new SocketsHttpHandler()
        {
            UseProxy = false,
            AllowAutoRedirect = false,
            AutomaticDecompression = DecompressionMethods.All,
            UseCookies = false,
            ActivityHeadersPropagator =
                new ReverseProxyPropagator(DistributedContextPropagator.Current),
        }
    );
    var requestConfig = new ForwarderRequestConfig { };

    if (context.Request.Path == "/")
    {
        context.Request.Path = "/index.html";
    }

    var error = await forwarder.SendAsync(
        context,
        ServiceDiscovery.ResolveService("management-app").ToString(),
        httpClient,
        requestConfig,
        HttpTransformer.Default
    );

    // Check if the operation was successful
    if (error != ForwarderError.None)
    {
        var errorFeature = context.GetForwarderErrorFeature();
        var exception = errorFeature?.Exception;
    }
}
