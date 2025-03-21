// Copyright (c) Duende Software. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System;
using BlazorServer.Data;
using IdentityModel.AspNetCore.AccessTokenManagement;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace BlazorServer
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        // For more information on how to configure your application, visit https://go.microsoft.com/fwlink/?LinkID=398940
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddRazorPages();
            services.AddServerSideBlazor();
            services.AddHttpContextAccessor();
            services.AddHttpClient();

            services.AddScoped<WeatherForecastService>();

            services.AddBff();

            services.AddHttpClient("api_client", configureClient =>
            {
                configureClient.BaseAddress = new Uri("https://localhost:5002/");
            });

            services.AddSingleton<IUserAccessTokenStore, CustomTokenStore>();

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

                    options.Events.OnTokenValidated = async n =>
                    {
                        var svc = n.HttpContext.RequestServices.GetRequiredService<IUserAccessTokenStore>();
                        var exp = DateTimeOffset.UtcNow.AddSeconds(double.Parse(n.TokenEndpointResponse.ExpiresIn));
                        await svc.StoreTokenAsync(n.Principal, n.TokenEndpointResponse.AccessToken, exp, n.TokenEndpointResponse.RefreshToken);
                    };
                });
        }

        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            else
            {
                app.UseExceptionHandler("/Error");
            }

            app.UseStaticFiles();

            app.UseRouting();
            app.UseAuthentication();
            app.UseBff();
            app.UseAuthorization();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapBffManagementEndpoints();

                endpoints.MapBlazorHub();
                endpoints.MapFallbackToPage("/_Host");
            });
        }
    }
}
