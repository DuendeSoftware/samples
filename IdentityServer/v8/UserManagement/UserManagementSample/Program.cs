// Copyright (c) Duende Software. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using Duende.IdentityServer;
using Duende.IdentityServer.Models;
using Duende.Storage.Schema;
using Duende.Storage.Sqlite;
using Duende.UserManagement;
using Duende.UserManagement.Authentication;
using Duende.UserManagement.Authentication.Passwords;
using Duende.UserManagement.Import;
using Duende.UserManagement.Profiles;
using UserManagementSample;
using UserManagementSample.Import;
using UserManagementSample.Services;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

builder.Services.AddRazorPages();

builder.Services.AddHttpContextAccessor();
builder.Services.AddSingleton<OtpCookie>();
builder.Services.AddSingleton<SecondFactorStateCookie>();

builder.Services
    // Add Identity server
    .AddIdentityServer()

    // Add and configure user management
    .AddUserManagement(um =>
    {
        // Configure authentication options. 
        um.Authentication(authentication =>
        {
            authentication.Configure(opt =>
            {
                opt.Passwords.MinLength = 8;

                // In order for the passkeys to be accepted, you'll need to configure the server domain and allowed origins. 
                opt.Passkeys.ServerDomain = "identityserver.dev.localhost";
                opt.Passkeys.AllowedOrigins = ["https://identityserver.dev.localhost:5001"];
                opt.Passkeys.RelyingPartyName = "UserManagement Sample";
            });

            // After signing in with the password, we set a cookie with the subject ID of the user.
            // This is then read by the second-factor resolver to link the second-factor attempt to the user that is signing in.
            // see https://docs.duendesoftware.com/identityserver/usermanagement/authentication/passkeys/#second-factor-passkey-authentication
            authentication.EnablePasskeyForSecondFactor<SecondFactorResolver>();

            // Adds configuration for sending OTP's. In this sample, we're sending OTP's via email,
            // but you could also implement a custom delivery mechanism, e.g. for sending OTP's via SMS.
            _ = authentication.UseSmtpOtpDispatcher(options => builder.Configuration.GetSection("Smtp").Bind(options));
            //authentication.UseSmtpOtpDispatcher(smtp =>
            //{
            //    var connectionString = builder.Configuration.GetConnectionString("mailpit");
            //    if (!string.IsNullOrWhiteSpace(connectionString) &&
            //        Uri.TryCreate(connectionString, UriKind.Absolute, out var uri))
            //    {
            //        smtp.Host = uri.Host;
            //        smtp.Port = uri.Port;
            //        smtp.EnableSsl = false;
            //    }
            //    else
            //    {
            //        smtp.Host = "localhost";
            //        smtp.Port = 1025;
            //        smtp.EnableSsl = false;
            //    }

            //    smtp.FromEmail = "no-reply@localhost";
            //    smtp.FromName = "UserManagement Sample";
            //});
        });

        // Store user management data in sql lite
        um.AddSqliteStore(opt =>
            opt.ConnectionString = "Data Source=../db/usermanagement.db");
    })
    .AddInMemoryClients([
        new Client
        {
            ClientId = "client",
            ClientSecrets = { new Secret("secret".Sha256()) },
            AllowedGrantTypes = GrantTypes.Code,
            RequirePkce = true,
            RedirectUris = { "https://client.dev.localhost:5002/signin-oidc" },
            PostLogoutRedirectUris = { "https://client.dev.localhost:5002/signout-callback-oidc" },
            FrontChannelLogoutUri = "https://client.dev.localhost:5002/signout-oidc",
            AllowedScopes =
            {
                IdentityServerConstants.StandardScopes.OpenId,
                IdentityServerConstants.StandardScopes.Profile,
            }
        }
    ])
    .AddInMemoryIdentityResources([
        new IdentityResources.OpenId(),
        new IdentityResources.Profile(),
    ]);

// Users from aspnet identity are imported with the passwords hashed via the ASP.NET Identity v3 format.
// After import, the passwords are re-hashed with the default hashing algorithm of Duende User Management, which is currently PBKDF2. 
builder.Services.AddSingleton<IPasswordHashAlgorithm, AspNetIdentityPasswordHashAlgorithm>();

// During import, by default, users with collisions (IE: duplicate subject id's or email addresses) will be skipped.
// By adding a custom collision resolver, you can change this behavior, e.g. to overwrite existing users instead of skipping them.
builder.Services.AddScoped<IUserImportConflictResolver, OverwriteConflictResolver>();

// Add the class that will import data from aspnet identity. 
builder.Services.AddScoped<AspNetIdentityImporter>(sp =>
{
    var connectionString = builder.Configuration.GetConnectionString("AspnetIdentitySource") ?? throw new InvalidOperationException("No connection string for AspnetIdentitySource configured");
    var platformImporter = sp.GetRequiredService<IUserImporter>();
    var profileAdmin = sp.GetRequiredService<IUserProfileAdmin>();
    return new AspNetIdentityImporter(platformImporter, profileAdmin, connectionString);
});


builder.Services.AddAuthentication()
    .AddOpenIdConnect("Google", "Sign-in with Google", options =>
    {
        options.SignInScheme = IdentityServerConstants.ExternalCookieAuthenticationScheme;
        options.ForwardSignOut = IdentityServerConstants.DefaultCookieAuthenticationScheme;

        options.Authority = "https://accounts.google.com/";
        options.ClientId = "708778530804-rhu8gc4kged3he14tbmonhmhe7a43hlp.apps.googleusercontent.com";

        options.CallbackPath = "/signin-google";
        options.Scope.Add("email");
        options.DisableTelemetry = true;
    });

var app = builder.Build();

app.MapDefaultEndpoints();

if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
    using var scope = app.Services.CreateScope();
    await scope.ServiceProvider.GetRequiredService<IDatabaseSchema>().MigrateAsync(CancellationToken.None);
    SeedData.EnsureSeedData(app);
}
else
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseIdentityServer();
app.UseAuthorization();

app.MapRazorPages();
app.MapUserManagement();

app.Run();
