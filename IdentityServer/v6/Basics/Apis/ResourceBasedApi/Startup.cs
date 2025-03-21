// Copyright (c) Duende Software. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using Client;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;

namespace ResourceBasedApi;

public class Startup
{
    public void ConfigureServices(IServiceCollection services)
    {
        services.AddControllers();

        services.AddCors();
        services.AddDistributedMemoryCache();

        services.AddAuthentication("token")

            // JWT tokens
            .AddJwtBearer("token", options =>
            {
                options.Authority = Urls.IdentityServer;
                options.Audience = "resource2";

                options.TokenValidationParameters.ValidTypes = new[] { "at+jwt" };

                // if token does not contain a dot, it is a reference token
                options.ForwardDefaultSelector = Selector.ForwardReferenceToken("introspection");
            })

            // reference tokens
            .AddOAuth2Introspection("introspection", options =>
            {
                options.Authority = Urls.IdentityServer;

                options.ClientId = "resource1";
                options.ClientSecret = "secret";
            });
    }

    public void Configure(IApplicationBuilder app)
    {
        app.UseRouting();
        app.UseAuthentication();
        app.UseAuthorization();

        app.UseEndpoints(endpoints =>
        {
            endpoints.MapControllers().RequireAuthorization();
        });
    }
}
