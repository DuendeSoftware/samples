// Copyright (c) Duende Software. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.Globalization;
using Duende.IdentityServer;
using Duende.IdentityServer.Stores;
using idunno.Security;
using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.IdentityModel.Tokens;
using Serilog;
using Serilog.Filters;

namespace CIMD.IdentityServer;

internal static class HostingExtensions
{
    public static WebApplicationBuilder ConfigureLogging(this WebApplicationBuilder builder)
    {
        // Write most logs to the console but diagnostic data to a file.
        // See https://docs.duendesoftware.com/identityserver/diagnostics/data
        builder.Host.UseSerilog((ctx, lc) =>
        {
            lc.WriteTo.Logger(consoleLogger =>
            {
                consoleLogger.WriteTo.Console(
                    outputTemplate:
                    "[{Timestamp:HH:mm:ss} {Level}] {SourceContext}{NewLine}{Message:lj}{NewLine}{Exception}{NewLine}",
                    formatProvider: CultureInfo.InvariantCulture);
                if (builder.Environment.IsDevelopment())
                {
                    consoleLogger.Filter.ByExcluding(Matching.FromSource("Duende.IdentityServer.Diagnostics.Summary"));
                }
            });
            if (builder.Environment.IsDevelopment())
            {
                lc.WriteTo.Logger(fileLogger =>
                {
                    fileLogger
                        .WriteTo.File("./diagnostics/diagnostic.log", rollingInterval: RollingInterval.Day,
                            fileSizeLimitBytes: 1024 * 1024 * 10, // 10 MB
                            rollOnFileSizeLimit: true,
                            outputTemplate:
                            "[{Timestamp:HH:mm:ss} {Level}] {SourceContext}{NewLine}{Message:lj}{NewLine}{Exception}{NewLine}",
                            formatProvider: CultureInfo.InvariantCulture)
                        .Filter
                        .ByIncludingOnly(Matching.FromSource("Duende.IdentityServer.Diagnostics.Summary"));
                }).Enrich.FromLogContext().ReadFrom.Configuration(ctx.Configuration);
            }
        });
        return builder;
    }

    public static WebApplication ConfigureServices(this WebApplicationBuilder builder)
    {
        builder.Services.AddRazorPages();

        var isBuilder = builder.Services.AddIdentityServer(options =>
            {
                options.Events.RaiseErrorEvents = true;
                options.Events.RaiseInformationEvents = true;
                options.Events.RaiseFailureEvents = true;
                options.Events.RaiseSuccessEvents = true;

                // Advertise CIMD support so clients (e.g., VS Code) know they can use
                // a Client ID Metadata Document URL as the client_id without DCR.
                options.Discovery.CustomEntries.Add("client_id_metadata_document_supported", true);

                // Use a large chunk size for diagnostic logs in development where it will be redirected to a local file
                if (builder.Environment.IsDevelopment())
                {
                    options.Diagnostics.ChunkSize = 1024 * 1024 * 10; // 10 MB
                }
            })
            .AddAppAuthRedirectUriValidator()
            .AddTestUsers(TestUsers.Users)
            .AddLicenseSummary();

        // in-memory, code config
        isBuilder.AddInMemoryIdentityResources(Config.IdentityResources);
        isBuilder.AddInMemoryApiScopes(Config.ApiScopes);
        isBuilder.AddInMemoryApiResources(Config.ApiResources);
        isBuilder.AddInMemoryClients(Config.Clients);

        // Add CIMD support so that CIMD clients are resolved dynamically
        // while statically configured clients still work.
        isBuilder.AddCimdClientStore<InMemoryClientStore>();

        builder.Services.AddAuthentication()
            .AddOpenIdConnect("oidc", "Sign-in with demo.duendesoftware.com", options =>
            {
                options.SignInScheme = IdentityServerConstants.ExternalCookieAuthenticationScheme;
                options.SignOutScheme = IdentityServerConstants.SignoutScheme;
                options.SaveTokens = true;

                options.Authority = "https://demo.duendesoftware.com";
                options.ClientId = "interactive.confidential";
                options.ClientSecret = "secret";
                options.ResponseType = "code";

                options.TokenValidationParameters = new TokenValidationParameters
                {
                    NameClaimType = "name",
                    RoleClaimType = "role"
                };
            });

        return builder.Build();
    }

    /// <summary>
    /// Adds CIMD (Client ID Metadata Document) support by stacking
    /// <see cref="CimdClientStore{T}"/> on top of the existing client store.
    /// </summary>
    /// <typeparam name="T">The concrete <see cref="IClientStore"/> implementation
    /// to decorate (e.g., <c>InMemoryClientStore</c>). This method automatically
    /// wraps it in <see cref="ValidatingClientStore{T}"/> before stacking the
    /// CIMD layer on top.</typeparam>
    public static IIdentityServerBuilder AddCimdClientStore<T>(
        this IIdentityServerBuilder builder) where T : class, IClientStore
    {
        builder.Services.AddHybridCache(options =>
        {
            options.DefaultEntryOptions = new HybridCacheEntryOptions
            {
                // Cache resolved CIMD clients for 15 minutes so that changes
                // to the metadata document are eventually picked up.
                Expiration = TimeSpan.FromMinutes(15),
                LocalCacheExpiration = TimeSpan.FromMinutes(15),
            };
        });
        builder.Services.AddSingleton<ICimdPolicy, McpCimdPolicy>();
        builder.Services.AddSingleton<SsrfGuard>();
        builder.Services.AddSingleton<CimdDocumentFetcher>();

        builder.Services.TryAddTransient(typeof(T));
        builder.Services.AddTransient<ValidatingClientStore<T>>();
        builder.Services.AddSingleton<IClientStore,
            CimdClientStore<ValidatingClientStore<T>>>();

        builder.Services.AddHttpClient(CimdDocumentFetcher.HttpClientName, client =>
            {
                // Limit how long we'll wait for a CIMD document to prevent
                // malicious servers from holding connections open indefinitely
                client.Timeout = TimeSpan.FromSeconds(5);
            })
            .ConfigurePrimaryHttpMessageHandler(sp =>
            {
                var env = sp.GetRequiredService<IHostEnvironment>();
                var ssrfGuard = sp.GetRequiredService<SsrfGuard>();

                // In development, when the server is on loopback, skip handler-level SSRF
                // protection. This is allowed in section 6.5 of the CIMD draft, and facilitates
                // running on localhost (as we do in this sample).
                if (env.IsDevelopment() && ssrfGuard.ServerIsOnLoopback())
                {
                    return new SocketsHttpHandler
                    {
                        // Per CIMD spec section 4: MUST NOT automatically follow HTTP redirects
                        AllowAutoRedirect = false
                    };
                }

                // In production, enable handler-level SSRF protection.
                // Note that the handler from idunno.Security.Ssrf disables auto redirect by default.
                return SsrfSocketsHttpHanderFactory.Create();
            });

        return builder;
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
