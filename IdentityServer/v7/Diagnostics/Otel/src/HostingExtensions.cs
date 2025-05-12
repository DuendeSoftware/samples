// Copyright (c) Duende Software. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using Duende.IdentityServer;
using Google.Apis.Auth.AspNetCore3;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using Serilog;

namespace Otel;

internal static class HostingExtensions
{
    public static WebApplication ConfigureServices(this WebApplicationBuilder builder)
    {
        builder.Services.AddRazorPages();

        var isBuilder = builder.Services.AddIdentityServer(options =>
            {
                options.Events.RaiseErrorEvents = true;
                options.Events.RaiseInformationEvents = true;
                options.Events.RaiseFailureEvents = true;
                options.Events.RaiseSuccessEvents = true;

                // see https://docs.duendesoftware.com/identityserver/v6/fundamentals/resources/
                options.EmitStaticAudienceClaim = true;
            })
            .AddTestUsers(TestUsers.Users);

        // in-memory, code config
        isBuilder.AddInMemoryIdentityResources(Config.IdentityResources);
        isBuilder.AddInMemoryApiScopes(Config.ApiScopes);
        isBuilder.AddInMemoryClients(Config.Clients);

        builder.Services.AddAuthentication()
            .AddGoogleOpenIdConnect(
                authenticationScheme: GoogleOpenIdConnectDefaults.AuthenticationScheme,
                displayName: "Google",
                configureOptions: options =>
                {
                    options.SignInScheme = IdentityServerConstants.ExternalCookieAuthenticationScheme;
  
                    // register your IdentityServer with Google at https://console.developers.google.com
                    // enable the Google+ API
                    // set the redirect URI to https://localhost:5001/signin-google
                    options.ClientId = "copy client ID from Google here";
                    options.ClientSecret = "copy client secret from Google here";
          
                    options.CallbackPath = "/signin-google";
                });

        builder.Services.AddOpenTelemetryTracing(builder =>
        {
            builder
                .AddConsoleExporter()

                // all avavilabe sources
                .AddSource(IdentityServerConstants.Tracing.Basic)
                .AddSource(IdentityServerConstants.Tracing.Cache)
                .AddSource(IdentityServerConstants.Tracing.Services)
                .AddSource(IdentityServerConstants.Tracing.Stores)
                .AddSource(IdentityServerConstants.Tracing.Validation)

                .SetResourceBuilder(
                    ResourceBuilder.CreateDefault()
                        .AddService("IdentityServerHost.Sample"))
                .AddHttpClientInstrumentation()
                .AddAspNetCoreInstrumentation()
                .AddSqlClientInstrumentation();
        });

        return builder.Build();
    }

    public static WebApplication ConfigurePipeline(this WebApplication app)
    {
        app.UseSerilogRequestLogging();

        if (app.Environment.IsDevelopment())
        {
            app.UseDeveloperExceptionPage();
        }

        app.UseStaticFiles();
        app.UseRouting();
        app.UseIdentityServer();
        app.UseAuthorization();

        app.MapRazorPages()
            .RequireAuthorization();

        return app;
    }
}
