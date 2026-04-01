using Duende.Bff;
using Duende.Bff.AccessTokenManagement;
using Duende.Bff.Yarp;
using Microsoft.AspNetCore.DataProtection;
using OpenApi.Bff;
using OpenApi.Bff.OpenApi;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

var bffConfig = builder.Configuration.GetSection("BFF");
builder.Services.AddBff()
    .AddRemoteApis()
    .LoadConfiguration(bffConfig);

// Add `.PersistKeysTo…()` and `.ProtectKeysWith…()` calls
// See more at https://docs.duendesoftware.com/general/data-protection
builder.Services.AddDataProtection()
    .SetApplicationName("BFF");

// Make sure Yarp understands aspire's service discovery.
builder.Services.AddHttpForwarderWithServiceDiscovery();

builder.Services.AddOpenApiDocumentsCombiner(opt =>
{
    opt.ServerUri = Services.Bff.ActualUri();
    opt.Documents = new[]
    {
        new OpenApiDocumentSource("/api1", new Uri(Services.Api1.LogicalUri(), "/openapi/v1.json")),
        new OpenApiDocumentSource("/api2", new Uri(Services.Api2.LogicalUri(), "/openapi/v1.json")),
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

app.MapDefaultEndpoints();

// Proxy all API's.
app.MapRemoteBffApiEndpoint("/api1", Services.Api1.LogicalUri())
    .WithAccessToken(RequiredTokenType.UserOrNone);
app.MapRemoteBffApiEndpoint("/api2", Services.Api2.LogicalUri())
    .WithAccessToken(RequiredTokenType.UserOrNone);

app.MapGet("/swagger/combined/v1.json",
    async (OpenApiDocumentCombiner c, CancellationToken ct) => await c.CombineDocuments(ct));

app.UseSwaggerUI(c =>
{
    // Inject a javascript function to add a CSRF header to all requests
    c.UseRequestInterceptor("function(request){ request.headers['X-CSRF'] = '1';return request;}");

    // Add some javascript that adds a login / logout button to the page.
    c.InjectJavascript("bff-auth-button.js");

    // Add all swagger endpoints for all APIs
    c.SwaggerEndpoint("/api1/openapi/v1.json", "API #1");
    c.SwaggerEndpoint("/api2/openapi/v1.json", "API #2");
    c.SwaggerEndpoint("/swagger/combined/v1.json", "Combined");
});

app.Run();
