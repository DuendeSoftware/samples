// Copyright (c) Duende Software. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using Duende.IdentityServer;
using Duende.IdentityServer.Models;
using Duende.Storage;
using Duende.Storage.Sqlite;
using Duende.UserManagement;
using Duende.UserManagement.Authentication;
using Duende.UserManagement.Authentication.Passwords;
using Duende.UserManagement.Import;
using Duende.UserManagement.Profiles;
using Microsoft.AspNetCore.Authentication.Google;
using UserManagementSample;
using UserManagementSample.Import;
using UserManagementSample.Services;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

builder.Services.AddRazorPages();

builder.Services.AddHttpContextAccessor();
builder.Services.AddSingleton<UserManagementSample.OtpCookie>();
builder.Services.AddSingleton<UserManagementSample.TotpStateCookie>();

builder.Services
    .AddIdentityServer()
    .AddUserManagement(users =>
    {
        users.Authentication(authentication =>
        {
            authentication.Configure(opt =>
            {
                opt.Passwords.MinLength = 8;

                opt.Passkeys.ServerDomain = "dev.localhost";
                opt.Passkeys.AllowedOrigins = ["https://um-identity-server.dev.localhost:59254"];
                opt.Passkeys.RelyingPartyName = "UserManagement Sample";
            });

            authentication.EnablePasskeyForSecondFactor<SecondFactorResolver>();

            authentication.UseSmtpOtpSender(smtp =>
            {
                var connectionString = builder.Configuration.GetConnectionString("mailpit");
                if (!string.IsNullOrWhiteSpace(connectionString) &&
                    Uri.TryCreate(connectionString, UriKind.Absolute, out var uri))
                {
                    smtp.Host = uri.Host;
                    smtp.Port = uri.Port;
                    smtp.EnableSsl = false;
                }
                else
                {
                    smtp.Host = "localhost";
                    smtp.Port = 1025;
                    smtp.EnableSsl = false;
                }

                smtp.FromEmail = "no-reply@localhost";
                smtp.FromName = "UserManagement Sample";
            });
        });

        users.AddSqliteStore(opt =>
            opt.ConnectionString = "Data Source=usermanagement.db");
    })
    .AddInMemoryClients([
        new Client
        {
            ClientId = "client",
            ClientSecrets = { new Secret("secret".Sha256()) },
            AllowedGrantTypes = GrantTypes.Code,
            RequirePkce = true,
            RedirectUris = { "https://um-client.dev.localhost:5002/signin-oidc" },
            PostLogoutRedirectUris = { "https://um-client.dev.localhost:5002/signout-callback-oidc" },
            FrontChannelLogoutUri = "https://um-client.dev.localhost:5002/signout-oidc",
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

builder.Services.AddSingleton<IPasswordHashAlgorithm, AspNetIdentityPasswordHashAlgorithm>();
builder.Services.AddScoped<IUserImportConflictResolver, OverwriteConflictResolver>();
builder.Services.AddScoped<ILocalUserImporter>(sp =>
{
    var connectionString = builder.Configuration.GetConnectionString("aspnetidentitysource")
        ?? "Data Source=aspnetidentitysource.db";
    var platformImporter = sp.GetRequiredService<IUserImporter>();
    var profileAdmin = sp.GetRequiredService<IUserProfileAdmin>();
    return new AspNetIdentityImporter(platformImporter, profileAdmin, connectionString);
});

builder.Services.AddAuthentication()
    .AddGoogle(GoogleDefaults.AuthenticationScheme, options =>
    {
        options.SignInScheme = IdentityServerConstants.ExternalCookieAuthenticationScheme;
        options.ClientId = builder.Configuration["Authentication:Google:ClientId"] ?? "not-configured";
        options.ClientSecret = builder.Configuration["Authentication:Google:ClientSecret"] ?? "not-configured";
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
