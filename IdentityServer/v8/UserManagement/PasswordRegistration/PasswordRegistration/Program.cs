// Copyright (c) Duende Software. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using Duende.IdentityServer;
using Duende.Storage.Schema;
using Duende.Storage.Sqlite;
using Duende.UserManagement;
using Duende.UserManagement.Authentication;
using Duende.UserManagement.Profiles;

using Microsoft.AspNetCore.Authentication.Cookies;

using PasswordRegistration;
using PasswordRegistration.PasswordValidators;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddRazorPages();

// Configure Authentication with Cookie-based sessions
builder.Services
    .AddAuthentication(options =>
    {
        options.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
        options.DefaultSignInScheme = CookieAuthenticationDefaults.AuthenticationScheme;
        options.DefaultAuthenticateScheme = CookieAuthenticationDefaults.AuthenticationScheme;
        options.DefaultChallengeScheme = CookieAuthenticationDefaults.AuthenticationScheme;
    })
    .AddCookie(CookieAuthenticationDefaults.AuthenticationScheme, options =>
    {
        options.Cookie.Name = "__Host-AuthSession";
        options.Cookie.HttpOnly = true;
        options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
        options.Cookie.SameSite = SameSiteMode.Lax;
        options.Cookie.IsEssential = true;
        options.ExpireTimeSpan = TimeSpan.FromHours(8);
        options.SlidingExpiration = true;
        options.LoginPath = "/Login";
        options.LogoutPath = "/Logout";
    });

builder.Services.AddAuthorization();

// TODO: switch to data protection key persistence appropriate for production
builder.Services.AddDataProtection();

var isBuilder = builder.Services.AddIdentityServer();
isBuilder.AddUserManagement(configure =>
    {
        _ = configure.Authentication(authentication =>
        {
            _ = authentication.Configure(authOptions =>
            {
                authOptions.Passwords.MinLength = 4;
                authOptions.Passwords.MinLower = 1;
                authOptions.Passwords.MinUpper = 1;
                authOptions.Passwords.MinDigits = 1;
                authOptions.Passwords.MinSymbols = 1;

                authOptions.Throttling.MaxFailedAttempts = 2;
                authOptions.Throttling.FailureWindow = TimeSpan.FromMinutes(5);
                authOptions.Throttling.ThrottleDuration = TimeSpan.FromMinutes(10);
            });

            _ = authentication.UseSmtpOtpDispatcher(options => builder.Configuration.GetSection("Smtp").Bind(options));
            _ = authentication.AddPasswordValidator<BlocklistPasswordValidator>();
        });

        configure.AddSqliteStore(o => o.ConnectionString = "Data Source=password-registration-sample.db");
    });

// OtpCookie adds the ability to store OTP-related data in a secure cookie between requests
builder.Services.AddHttpContextAccessor();
builder.Services.AddSingleton<OtpCookie>();

var app = builder.Build();

var cts = new CancellationTokenSource();

Console.CancelKeyPress += (_, e) =>
{
    e.Cancel = true;
    cts.Cancel();
};

// Initialize Persona database
using var scope = app.Services.CreateScope();
await scope.ServiceProvider.GetRequiredService<IDatabaseSchema>().MigrateAsync(cts.Token);

// Bootstrap Persona user schema with custom attributes
await scope.ServiceProvider.GetRequiredService<IUserProfileSchemaAdmin>().BootstrapAsync(cts.Token);

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    _ = app.UseExceptionHandler("/Error");
    _ = app.UseHsts();
}

app.UseHttpsRedirection();

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapStaticAssets();
app.MapRazorPages()
    .WithStaticAssets();

app.Run();
