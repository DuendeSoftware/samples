// Copyright (c) Duende Software. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using Duende.Bff;
using Duende.Bff.Yarp;

namespace BackendHost;

public class Startup
{
    public void ConfigureServices(IServiceCollection services)
    {
        services.AddCors(opt =>
        {
            opt.AddDefaultPolicy(policy =>
            {
                policy
                    .WithOrigins("https://localhost:5011")
                    .WithHeaders("x-csrf", "content-type")
                    .WithMethods("DELETE")
                    .AllowCredentials();
            });
        });
        services.AddControllers();
        services.AddBff()
            .AddRemoteApis();
        services.AddTransient<IReturnUrlValidator, FrontendHostReturnUrlValidator>();

        // registers HTTP client that uses the managed user access token
        services.AddUserAccessTokenHttpClient("api_client", configureClient: client =>
        {
            client.BaseAddress = new Uri("https://localhost:5002/");
        });

        services.AddAuthentication(options =>
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
    }

    public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
    {
        app.UseDefaultFiles();
        app.UseStaticFiles();

        app.UseRouting();
        app.UseCors();

        app.UseAuthentication();
        app.UseBff();
        app.UseAuthorization();

        app.UseEndpoints(endpoints =>
        {
            endpoints.MapBffManagementEndpoints();

            // if you want the TODOs API local
            endpoints.MapControllers()
                .RequireAuthorization()
                .AsBffApiEndpoint();

            // if you want the TODOs API remote
            // endpoints.MapRemoteBffApiEndpoint("/todos", "https://localhost:5020/todos")
            //     .RequireAccessToken(Duende.Bff.TokenType.User);
        });
    }
}
