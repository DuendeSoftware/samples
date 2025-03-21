// Copyright (c) Duende Software. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.


using System.Security.Cryptography.X509Certificates;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace IdentityServerHost;

public class Startup
{
    public IWebHostEnvironment Environment { get; }
    public IConfiguration Configuration { get; }

    public Startup(IWebHostEnvironment environment, IConfiguration configuration)
    {
        Environment = environment;
        Configuration = configuration;
    }

    public void ConfigureServices(IServiceCollection services)
    {
        services.AddRazorPages();

        var builder = services.AddIdentityServer(options =>
        {
            options.Events.RaiseErrorEvents = true;
            options.Events.RaiseInformationEvents = true;
            options.Events.RaiseFailureEvents = true;
            options.Events.RaiseSuccessEvents = true;

            // see https://docs.duendesoftware.com/identityserver/v5/basics/resources
            options.EmitStaticAudienceClaim = true;

            // MTLS stuff
            options.MutualTls.Enabled = true;
            options.MutualTls.AlwaysEmitConfirmationClaim = true;
            options.MutualTls.DomainName = "mtls.localhost:5099";
            // set this to be explicit when using a domain name for mTLS
            options.IssuerUri = "https://localhost:5001";
        });

        builder.AddTestUsers(TestUsers.Users);
        builder.AddInMemoryClients(Clients.List);
        builder.AddInMemoryIdentityResources(Resources.Identity);
        builder.AddInMemoryApiScopes(Resources.ApiScopes);

        // this allows MTLS to be used as client authentication
        builder.AddMutualTlsSecretValidators();

        // for local testing, we will use kestrel's MTLS
        // this requires DNS to be setup -- hosts file would contain:
        // 127.0.0.1 ::1 mtls.localhost
        var mtls_localhost = new X509Certificate2("mtls.localhost.pfx", "password");
        services.Configure<KestrelServerOptions>(options =>
        {
            options.ListenLocalhost(5001, config => config.UseHttps());
            options.ListenLocalhost(5099, config =>
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
        if (Environment.IsDevelopment())
        {
            app.UseDeveloperExceptionPage();
        }

        app.UseStaticFiles();

        app.UseRouting();
        app.UseIdentityServer();
        app.UseAuthorization();
        app.UseEndpoints(endpoints =>
        {
            endpoints.MapRazorPages();
        });
    }
}
