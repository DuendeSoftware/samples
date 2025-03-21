// Copyright (c) Duende Software. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.IdentityModel.Tokens.Jwt;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;

namespace Client;

public class Startup
{
    public void ConfigureServices(IServiceCollection services)
    {
        JwtSecurityTokenHandler.DefaultMapInboundClaims = false;

        services.AddControllersWithViews();
        services.AddHttpClient();

        // implements the cookie event handler
        services.AddTransient<CookieEventHandler>();

        // demo version of a state management to keep track of logout notifications
        services.AddSingleton<LogoutSessionManager>();

        services.AddAuthentication(options =>
        {
            options.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
            options.DefaultChallengeScheme = "oidc";
        })
            .AddCookie(options =>
            {
                options.EventsType = typeof(CookieEventHandler);
            })
            .AddOpenIdConnect("oidc", options =>
            {
                options.Authority = Urls.IdentityServer;
                options.RequireHttpsMetadata = false;

                options.ClientId = "mvc.backchannel.sample";
                options.ClientSecret = "secret";

                options.ResponseType = "code";

                options.Scope.Clear();
                options.Scope.Add("openid");
                options.Scope.Add("profile");
                options.Scope.Add("scope1");
                options.Scope.Add("offline_access");

                // not mapped by default
                options.ClaimActions.MapJsonKey("website", "website");

                // keeps id_token smaller
                options.GetClaimsFromUserInfoEndpoint = true;
                options.SaveTokens = true;

                options.TokenValidationParameters = new TokenValidationParameters
                {
                    NameClaimType = "name",
                    RoleClaimType = "role"
                };
            });
    }

    public void Configure(IApplicationBuilder app)
    {
        app.UseDeveloperExceptionPage();
        app.UseStaticFiles();

        app.UseRouting();

        app.UseAuthentication();
        app.UseAuthorization();

        app.UseEndpoints(endpoints =>
        {
            endpoints.MapDefaultControllerRoute()
                .RequireAuthorization();
        });
    }
}
