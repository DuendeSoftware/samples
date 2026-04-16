// Copyright (c) Duende Software. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using Duende.IdentityServer;
using IdentityServerHost;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;

var seed = args.Contains("/seed");
if (seed)
{
    args = args.Except(new[] { "/seed" }).ToArray();
}

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");

if (seed)
{
    SeedData.EnsureSeedData(connectionString);
    return;
}

builder.Services.AddRazorPages();

var idsvrBuilder = builder.Services.AddIdentityServer(options =>
{
    // see https://docs.duendesoftware.com/identityserver/fundamentals/resources
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
    .AddOpenIdConnect("oidc", "Sign-in with demo.duendesoftware.com", options =>
    {
        options.SignInScheme = IdentityServerConstants.ExternalCookieAuthenticationScheme;
        options.SignOutScheme = IdentityServerConstants.SignoutScheme;
        options.SaveTokens = true;

        options.Authority = "https://demo.duendesoftware.com";
        options.ClientId = "interactive.confidential";
        options.ClientSecret = "secret";
        options.ResponseType = "code";

        options.TokenValidationParameters = new()
        {
            NameClaimType = "name",
            RoleClaimType = "role"
        };
    });

// Add `.PersistKeysTo…()` and `.ProtectKeysWith…()` calls
// See more at https://docs.duendesoftware.com/general/data-protection
builder.Services.AddDataProtection()
    .SetApplicationName("IdentityServer");

var app = builder.Build();

app.MapDefaultEndpoints();

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
