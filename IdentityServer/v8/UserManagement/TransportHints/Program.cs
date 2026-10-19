// Copyright (c) Duende Software. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using Duende.IdentityServer;
using Duende.Storage.Schema;
using Duende.Storage.Sqlite;
using Duende.UserManagement;
using Duende.UserManagement.Authentication;
using Duende.UserManagement.Authentication.Otp;
using GettingStarted;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.DataProtection;
using TransportHints;

var builder = WebApplication.CreateBuilder(args);

builder.Services
    .AddRazorPages(options => options.Conventions.AuthorizePage("/Index"))

    // Set a unique cookie name for the TempData provider to avoid conflicts with other apps.
    .AddCookieTempDataProvider(options => options.Cookie.Name = "TransportHints.TempData");

builder.Services
    .AddHttpContextAccessor();

builder.Services
    .AddDataProtection()
    .SetApplicationName("UserManagement-TransportHints");

// Add an IOtpDispatcher that uses ConsoleOtpDispatcher for demo purposes.
// OTP's will be written to the console instead of being sent via email or SMS.
builder.Services.AddSingleton<IOtpDispatcher, ConsoleOtpDispatcher>();

// Configure the cookie authentication options for IdentityServer to use a unique cookie name.
builder.Services.PostConfigure<CookieAuthenticationOptions>(
    IdentityServerConstants.DefaultCookieAuthenticationScheme,
    options => options.Cookie.Name = "TransportHints.Session"
);

builder.Services
    .AddIdentityServer()
    .AddDeveloperSigningCredential(persistKey: false)
    .AddStorage(storage => storage.AddSqliteInMemory())
    .AddUserManagement(userManagement => userManagement.Authentication(authentication =>
    {
        authentication.Configure(options =>
        {
            options.Passkeys.ServerDomain = "localhost";
            options.Passkeys.AllowedOrigins = ["https://localhost:5017"];
            options.Passkeys.RelyingPartyName = "Transport Hints Sample";
        });

        authentication.EnablePasskeyForSecondFactor<SignedInUserResolver>();
    }));

var app = builder.Build();
using (var scope = app.Services.CreateScope())
{
    await scope.ServiceProvider
        .GetRequiredService<IStorageInstanceSchema>()
        .MigrateAsync(CancellationToken.None);
}

app.UseHttpsRedirection();
app.UseRouting();
app.UseIdentityServer();
app.UseAuthorization();
app.MapStaticAssets();
app.MapRazorPages();
app.MapUserManagement();
app.Run();
