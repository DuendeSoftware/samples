// Copyright (c) Duende Software. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System;
using System.Collections.Generic;
using Duende.Bff;
using Duende.Bff.Yarp;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Yarp.ReverseProxy.Configuration;

namespace FrontendHost
{
    public class Startup
    {
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddControllers();

            services.AddBff();

            var builder = services.AddReverseProxy()
                .AddTransforms<AccessTokenTransformProvider>();

            builder.LoadFromMemory(
                new[]
                {
                    new RouteConfig()
                    {
                        RouteId = "todos",
                        ClusterId = "cluster1",

                        Match = new RouteMatch
                        {
                            Path = "/todos/{**catch-all}"
                        }
                    }.WithAccessToken(TokenType.User),
                },
                new[]
                {
                    new ClusterConfig
                    {
                        ClusterId = "cluster1",

                        Destinations = new Dictionary<string, DestinationConfig>(StringComparer.OrdinalIgnoreCase)
                        {
                            { "destination1", new DestinationConfig() { Address = "https://localhost:5020" } },
                        }
                    }
                });

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

            app.UseAuthentication();
            app.UseBff();
            app.UseAuthorization();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapBffManagementEndpoints();

                // if you want the TODOs API local
                // endpoints.MapControllers()
                //     .RequireAuthorization()
                //     .AsBffApiEndpoint();

                // if you want the TODOs API remote
                endpoints.MapBffReverseProxy();

                // which is equivalent to
                //endpoints.MapReverseProxy()
                //    .AsBffApiEndpoint();
            });
        }
    }
}
