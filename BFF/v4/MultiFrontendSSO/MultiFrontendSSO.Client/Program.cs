using Duende.Bff;
using Duende.Bff.DynamicFrontends;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.Razor;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.ViewFeatures;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddHttpContextAccessor();
builder.Services.AddHttpClient();
builder.Services.AddRazorPages();
builder.Services
    .AddBff()
    .AddServerSideSessions()
    .ConfigureOpenIdConnect(options =>
    {
        options.Authority = "https://localhost:5001";
        options.ClientSecret = "secret";
        options.ResponseType = "code";
        options.ResponseMode = "query";

        options.GetClaimsFromUserInfoEndpoint = true;
        options.SaveTokens = true;
        options.MapInboundClaims = false;

        options.Scope.Clear();
        options.Scope.Add("openid");
        options.Scope.Add("profile");
        options.Scope.Add("offline_access");
    })
    .AddFrontends(
        new BffFrontend(BffFrontendName.Parse("frontend1"))
            .WithOpenIdConnectOptions(x => { x.ClientId = "frontend1"; })
            .MapToPath("/frontend1"),
        new BffFrontend(BffFrontendName.Parse("frontend2"))
            .WithOpenIdConnectOptions(x => { x.ClientId = "frontend2"; })
            .MapToPath("/frontend2")
    );

builder.Services.AddAuthorization();

var app = builder.Build();

app.UseAuthentication();
app.UseRouting();

app.UseBff();

app.UseAuthorization();

app.MapGet("/", async (
    [FromServices] ITempDataProvider tempDataProvider,
    [FromServices] IHttpContextAccessor httpContextAccessor,
    [FromServices] IRazorViewEngine razorViewEngine) =>
{
    var httpContext = httpContextAccessor.HttpContext!;
    var actionContext = new ActionContext(httpContext, httpContext.GetRouteData(),
        new Microsoft.AspNetCore.Mvc.Abstractions.ActionDescriptor());

    var viewResult = razorViewEngine.FindView(actionContext, "Index", false);

    if (!viewResult.Success)
    {
        return Results.NotFound();
    }

    var viewData = new ViewDataDictionary(new EmptyModelMetadataProvider(), new ModelStateDictionary())
    {
        Model = new { Message = "Hello from MapGet!" }
    };

    var tempData = new TempDataDictionary(httpContext, tempDataProvider);

    await using var writer = new StringWriter();
    var viewContext = new ViewContext(
        actionContext,
        viewResult.View,
        viewData,
        tempData,
        writer,
        new HtmlHelperOptions()
    );

    await viewResult.View.RenderAsync(viewContext);

    return Results.Content(writer.ToString(), "text/html");
});
app.MapStaticAssets();
app.Run();
