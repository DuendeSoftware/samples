// Copyright (c) Duende Software. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.IdentityModel.Tokens.Jwt;
using System.Security.Cryptography.X509Certificates;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.Extensions.DependencyInjection;

namespace SampleApi;

public class Startup
{
    public Startup()
    {
        JwtSecurityTokenHandler.DefaultMapInboundClaims = false;
    }

    public void ConfigureServices(IServiceCollection services)
    {
        services.AddControllers();

        // this API will accept any access token from the authority
        services.AddAuthentication("token")
            .AddJwtBearer("token", options =>
            {
                options.Authority = "https://localhost:5001";
                options.TokenValidationParameters.ValidateAudience = false;

                options.TokenValidationParameters.ValidTypes = new[] { "at+jwt" };
            });

        // for local testing, we will use kestrel's MTLS
        // this requires DNS to be setup -- hosts file would contain:
        // 127.0.0.1 ::1 api.localhost
        var mtls_localhost = new X509Certificate2("api.localhost.pfx", "password");
        services.Configure<KestrelServerOptions>(options =>
        {
            options.ListenLocalhost(6001, config => config.UseHttps());
            options.ListenLocalhost(6002, config =>
            {
                config.UseHttps(https =>
                {
                    https.ClientCertificateMode = Microsoft.AspNetCore.Server.Kestrel.Https.ClientCertificateMode.RequireCertificate;
                    https.AllowAnyClientCertificate();
                    https.ServerCertificate = mtls_localhost;
                });
            });
        });

    }

    public void Configure(IApplicationBuilder app)
    {
        app.UseRouting();

        app.UseAuthentication();
        app.UseConfirmationValidation();

        app.UseAuthorization();

        app.UseEndpoints(endpoints =>
        {
            endpoints.MapControllers().RequireAuthorization();
        });
    }
}
