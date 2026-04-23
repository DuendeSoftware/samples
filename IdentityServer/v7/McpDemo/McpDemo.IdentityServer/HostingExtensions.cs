using System.Globalization;
using Duende.IdentityServer;
using Duende.IdentityServer.Configuration;
using Duende.IdentityServer.Configuration.Validation.DynamicClientRegistration;
using Duende.IdentityServer.Stores;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.IdentityModel.Tokens;

namespace McpDemo.IdentityServer;

internal static class HostingExtensions
{

    public static WebApplication ConfigureServices(this WebApplicationBuilder builder)
    {
        builder.Services.AddRazorPages();

        var isBuilder = builder.Services.AddIdentityServer(options =>
            {
                // this will add the default dynamic client registration endpoint to the discovery/metadatada documents
                options.Discovery.DynamicClientRegistration.RegistrationEndpointMode = RegistrationEndpointMode.Inferred;
            })
            .AddTestUsers(TestUsers.Users)
            .AddLicenseSummary();

        // in-memory, code config
        isBuilder.AddInMemoryIdentityResources(Config.IdentityResources);
        isBuilder.AddInMemoryApiScopes(Config.ApiScopes);
        // since this will use DCR, we do not need any pre-configured clients
        isBuilder.AddInMemoryClients([]);
        isBuilder.AddInMemoryApiResources(Config.ApiResources);

        builder.Services.AddIdentityServerConfiguration(_ => { })
            // in memory is being used here to keep the demo simple. in a real scenario, a persistent storage
            // mechanism is needed for client registrations to persist across application restarts
            .AddInMemoryClientConfigurationStore();

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

        // Add `.PersistKeysTo…()` and `.ProtectKeysWith…()` calls
        // See more at https://docs.duendesoftware.com/general/data-protection
        builder.Services.AddDataProtection()
            .SetApplicationName("IdentityServer");

        return builder.Build();
    }

    public static WebApplication ConfigurePipeline(this WebApplication app)
    {
        app.MapDefaultEndpoints();

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

        app.MapDynamicClientRegistration();

        return app;
    }
}
