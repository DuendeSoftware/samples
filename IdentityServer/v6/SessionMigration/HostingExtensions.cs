// Copyright (c) Duende Software. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using Serilog;

namespace SessionMigration;

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

        // ********************
        // *** INSTRUCTIONS ***
        // This purpose of this sample is to show how to migrate existing client side session to server
        // side session without the user having to login again. To test it, first run the sample as is
        // and log in (user: alice, password: alice) to get a normal cookie based session in your
        // browser. Then uncomment the following blocks and run the sample again. The first request
        // from your browser to IdentityServer will cause the session to be migrated to be a server
        // side session.
        // Note that further restarts will invalidate your session as the server side session
        // store in this sample is in memory only.
        // Note that if server side sessions have been enabled and then are removed before another
        // test run, you have to manually clear the cookie to be able to log in again.

        // ** This is the normal template code for activating server side sessions.

        //isBuilder.AddServerSideSessions();

        //builder.Services.AddAuthorization(options =>
        //       options.AddPolicy("admin",
        //           policy => policy.RequireClaim("sub", "1"))
        //   );
        //builder.Services.Configure<RazorPagesOptions>(options =>
        //    options.Conventions.AuthorizeFolder("/ServerSideSessions", "admin"));

        // ** This is the code that adds migration of sessions. Enabling server side sessions through the
        // ** block above without enabling this will invalidate all existing sessions. 
        //builder.Services.AddTransient<IPostConfigureOptions<CookieAuthenticationOptions>, SessionMigrationPostConfigureOptions>();

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
