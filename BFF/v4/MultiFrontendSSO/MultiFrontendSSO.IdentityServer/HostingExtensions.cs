// Copyright (c) Duende Software. All rights reserved.
// See LICENSE in the project root for license information.

using IdentityServer;
using Microsoft.AspNetCore.DataProtection;
using Serilog;

namespace MultiFrontendSSO.IdentityServer;

public static class HostingExtensions
{
    public static WebApplication ConfigureServices(this WebApplicationBuilder builder)
    {
        builder.Services.AddRazorPages();

        var isBuilder = builder.Services.AddIdentityServer(
                options =>
                {
                    options.Events.RaiseErrorEvents = true;
                    options.Events.RaiseInformationEvents = true;
                    options.Events.RaiseFailureEvents = true;
                    options.Events.RaiseSuccessEvents = true;
                    options.EmitStaticAudienceClaim = true;
                })
            .AddTestUsers(TestUsers.Users)
            .AddRedirectUriValidator<AllowAnyRedirectUriValidator>();

        // in-memory, code config
        isBuilder.AddInMemoryIdentityResources(Config.IdentityResources);
        isBuilder.AddInMemoryApiScopes(Config.ApiScopes);
        isBuilder.AddInMemoryClients(Config.Clients);
        isBuilder.AddInMemoryApiResources(Config.ApiResources);

        builder.Services
            .AddAuthentication();

        // Add `.PersistKeysTo…()` and `.ProtectKeysWith…()`calls
        // See more at https://docs.duendesoftware.com/general/data-protection
        builder.Services.AddDataProtection()
            .SetApplicationName("BFF");

        return builder.Build();
    }

    public static WebApplication ConfigurePipeline(this WebApplication app)
    {
        app.UseSerilogRequestLogging();


        if (app.Environment.IsDevelopment())
        {
            app.UseDeveloperExceptionPage();
        }

        app.MapStaticAssets();
        app.UseRouting();
        app.UseIdentityServer();
        app.UseAuthorization();

        app.MapRazorPages()
            .RequireAuthorization()
            .WithStaticAssets();

        return app;
    }
}
