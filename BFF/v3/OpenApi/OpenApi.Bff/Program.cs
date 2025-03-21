using OpenApi.Bff;
using Duende.Bff.Yarp;
using Yarp.ReverseProxy.Transforms;
using Microsoft.OpenApi;
using Microsoft.OpenApi.Models;
using Microsoft.OpenApi.Extensions;
using Microsoft.OpenApi.Any;
using Microsoft.OpenApi.Readers;
using Duende.Bff;
using Microsoft.AspNetCore.Authorization;

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

builder.Services.AddAuthorization(opt =>
{
    opt.DefaultPolicy = new AuthorizationPolicyBuilder().RequireAuthenticatedUser().Build();
});

builder.Services.AddSingleton<BffYarpTransformBuilder>((path, c) =>
{
    DefaultBffYarpTransformerBuilders.DirectProxyWithAccessToken(path, c);
    c.ResponseTransforms.Add(new OpenApiResponseTransform(path));
});


var app = builder.Build();

app.UseDefaultFiles();
app.UseStaticFiles();

app.Use(async (c, n) =>
{
    //if (c.Request.Path.ToString().EndsWith("/openapi/v1.json"))
    //{
    //    c.Request.Headers"X-CSRF", "1");
    //}
    await n();
});

app.UseAuthentication();
app.UseBff();

app.MapBffManagementEndpoints();

app.MapRemoteBffApiEndpoint("/api1", Services.Api1.LogicalUri().ToString())
    .WithOptionalUserAccessToken();
app.MapRemoteBffApiEndpoint("/api2", Services.Api2.LogicalUri().ToString())
    .WithOptionalUserAccessToken();

app.UseAuthorization();
app.UseSwaggerUI(c =>
{

    c.UseRequestInterceptor("function(request){ request.headers['X-CSRF'] = '1';return request;}");

    c.InjectJavascript("bff-auth-button.js");
    c.SwaggerEndpoint("/api1/openapi/v1.json", "Api1");
    c.SwaggerEndpoint("/api2/openapi/v1.json", "Api2");
});

app.Run();

public class OpenApiResponseTransform(string basePath) : ResponseTransform
{
    public override async ValueTask ApplyAsync(ResponseTransformContext context)
    {
        // Check if the request path matches /openapi/{document}.json
        if (context.HttpContext.Request.Path.StartsWithSegments(basePath +"/openapi", out var remainingPath) &&
            remainingPath.HasValue && remainingPath.Value.EndsWith(".json"))
        {
            var readAsStreamAsync = await context.ProxyResponse.Content.ReadAsStreamAsync();
            var doc = new OpenApiStreamReader().Read(readAsStreamAsync, out var diagnostic);
            context.SuppressResponseBody = true;


            doc.Servers.Clear();
            doc.Servers.Add(new OpenApiServer()
            {
                Url = new Uri(Services.Bff.ActualUri(), basePath).ToString()
            });

            // remove the jwt security scheme
            doc.Components.SecuritySchemes.Clear();
            // Add the cookie security scheme
            //doc.Components.SecuritySchemes.Add("cookieAuth", new OpenApiSecurityScheme
            //{
            //    Type = SecuritySchemeType.Http,
            //    In = ParameterLocation.Cookie,
            //    Name = "__Host-bff",
            //    Description = "Cookie-based authentication"
            //});

            foreach (var path in doc.Paths)
            {
                foreach (var operation in path.Value.Operations)
                {
                    //operation.Value.Responses.Add("401", new OpenApiResponse()
                    //{
                    //    Description = "Unauthorized"
                    //});
                    //operation.Value.Parameters.Add(new OpenApiParameter()
                    //{
                    //    In = ParameterLocation.Header,
                    //    Name = "X-CSRF",
                    //    Required = true,
                    //    Schema = new OpenApiSchema()
                    //    {
                    //        Type = "string",
                    //        Default = new OpenApiString("1")
                    //    }

                    //});
                }
            }
            // Read and parse the existing JSON content

            var memoryStream = new MemoryStream();
            doc.Serialize(memoryStream, OpenApiSpecVersion.OpenApi3_0, OpenApiFormat.Json);
            memoryStream.Position = 0;
            await memoryStream.CopyToAsync(context.HttpContext.Response.Body);
            await context.HttpContext.Response.Body.FlushAsync();

        }
    }
}


