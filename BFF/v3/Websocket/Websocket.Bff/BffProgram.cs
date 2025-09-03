using Duende.Bff;
using Duende.Bff.AccessTokenManagement;
using Duende.Bff.Yarp;
using Microsoft.IdentityModel.JsonWebTokens;

var builder = WebApplication.CreateBuilder(args);

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
    .AddRemoteApis();


var app = builder.Build();

app.UseHttpsRedirection();


app.UseAuthentication();
app.UseRouting();
app.UseBff();

// adds authorization for local and remote API endpoints
//app.UseAuthorization();
    //app.MapGet("/", () => "ok");

app.UseWebSockets();

app.MapRemoteBffApiEndpoint("/graphql", new Uri("http://localhost:5095/graphql"))
    .WithAccessToken(RequiredTokenType.User);

//app.MapBffManagementEndpoints();

app.MapRemoteBffApiEndpoint("/", new Uri("http://localhost:5173"))
    .WithAccessToken(RequiredTokenType.None)
    .SkipAntiforgery();

app.Run();

