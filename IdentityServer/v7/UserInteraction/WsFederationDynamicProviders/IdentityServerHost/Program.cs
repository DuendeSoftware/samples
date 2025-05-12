// Copyright (c) Duende Software. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using Duende.IdentityServer;
using Google.Apis.Auth.AspNetCore3;
using IdentityServerHost;
using Microsoft.EntityFrameworkCore;
using Serilog;
using Serilog.Events;
using Serilog.Sinks.SystemConsole.Themes;

Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Debug()
    .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
    .MinimumLevel.Override("Microsoft.Hosting.Lifetime", LogEventLevel.Information)
    .MinimumLevel.Override("System", LogEventLevel.Warning)
    .MinimumLevel.Override("Microsoft.AspNetCore.Authentication", LogEventLevel.Information)
    .Enrich.FromLogContext()
    // uncomment to write to Azure diagnostics stream
    //.WriteTo.File(
    //    @"D:\home\LogFiles\Application\identityserver.txt",
    //    fileSizeLimitBytes: 1_000_000,
    //    rollOnFileSizeLimit: true,
    //    shared: true,
    //    flushToDiskInterval: TimeSpan.FromSeconds(1))
    .WriteTo.Console(outputTemplate: "[{Timestamp:HH:mm:ss} {Level}] {SourceContext}{NewLine}{Message:lj}{NewLine}{Exception}{NewLine}", theme: AnsiConsoleTheme.Code)
    .CreateLogger();

var seed = args.Contains("/seed");
if (seed)
{
    args = args.Except(new[] { "/seed" }).ToArray();
}

var builder = WebApplication.CreateBuilder(args);

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");

if (seed)
{
    Log.Information("Seeding database...");
    SeedData.EnsureSeedData(connectionString);
    Log.Information("Done seeding database.");
    return;
}

builder.Services.AddRazorPages();

var idsvrBuilder = builder.Services.AddIdentityServer(options =>
{
    options.Events.RaiseErrorEvents = true;
    options.Events.RaiseInformationEvents = true;
    options.Events.RaiseFailureEvents = true;
    options.Events.RaiseSuccessEvents = true;

    // see https://docs.duendesoftware.com/identityserver/v5/fundamentals/resources/
    options.EmitStaticAudienceClaim = true;
})
    .AddTestUsers(TestUsers.Users)
    // this adds the config data from DB (clients, resources, CORS)
    .AddConfigurationStore(options =>
    {
        options.ConfigureDbContext = b =>
            b.UseSqlite(connectionString, dbOpts => dbOpts.MigrationsAssembly(typeof(Program).Assembly.FullName));
    })
    // this adds the operational data from DB (codes, tokens, consents)
    .AddOperationalStore(options =>
    {
        options.ConfigureDbContext = b =>
            b.UseSqlite(connectionString, dbOpts => dbOpts.MigrationsAssembly(typeof(Program).Assembly.FullName));

        // this enables automatic token cleanup. this is optional.
        options.EnableTokenCleanup = true;
    });

idsvrBuilder.AddWsFedDynamicProvider()
    .AddIdentityProviderStore<EfWsFedProviderStore>();

//idsvrBuilder.AddWsFedDynamicProvider()
//  .AddInMemoryWsFedProviders(new WsFedProvider[] {
//        new WsFedProvider
//        {
//            Scheme = "adfs",
//            MetadataAddress = "https://adfs4.local/federationmetadata/2007-06/federationmetadata.xml",
//            RelyingPartyId = "urn:test",
//            DisplayName = "Local ADFS"
//        }
//    });


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

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
}

app.UseStaticFiles();

app.UseRouting();
app.UseIdentityServer();
app.UseAuthorization();
app.MapRazorPages();

app.Run();
