// Copyright (c) Duende Software. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System;
using System.IdentityModel.Tokens.Jwt;
using IdentityModel.AspNetCore.AccessTokenManagement;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;

namespace Client
{
    public class Startup
    {
        private readonly IConfiguration _configuration;

        public Startup(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public void ConfigureServices(IServiceCollection services)
        {
            JwtSecurityTokenHandler.DefaultMapInboundClaims = false;

            services.AddControllersWithViews();
            services.AddHttpClient();

            services.AddAuthentication(options =>
            {
                options.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = "oidc";
            })
                .AddCookie(options =>
                {
                    options.Cookie.Name = "mvc";

                    options.Events.OnSigningOut = async e =>
                    {
                        // automatically revoke refresh token at signout time
                        await e.HttpContext.RevokeUserRefreshTokenAsync();
                    };
                })
                .AddOpenIdConnect("oidc", options =>
                {
                    options.Authority = Urls.IdentityServer;

                    // no static client secret
                    // the secret id created dynamically
                    options.ClientId = _configuration.GetValue<string>("ClientId");

                    // needed to add JWR / private_key_jwt support
                    options.EventsType = typeof(OidcEvents);

                    // code flow + PKCE (PKCE is turned on by default)
                    options.ResponseType = "code";
                    options.UsePkce = true;

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

            // add service to create JWTs
            services.AddSingleton<AssertionService>();

            // add event handler for OIDC events
            services.AddTransient<OidcEvents>();

            // add automatic token management
            services.AddAccessTokenManagement();

            // add service to create assertions for token management
            services.AddTransient<ITokenClientConfigurationService, AssertionConfigurationService>();

            // add HTTP client to call protected API
            services.AddUserAccessTokenClient("client", client =>
            {
                client.BaseAddress = new Uri(Urls.SampleApi);
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
}
