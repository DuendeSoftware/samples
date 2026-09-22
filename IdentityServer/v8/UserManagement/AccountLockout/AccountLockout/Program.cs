// Copyright (c) Duende Software. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using AccountLockout;
using AccountLockout.Services;
using Duende.IdentityModel;
using Duende.IdentityServer;
using Duende.Storage.Schema;
using Duende.Storage.Sqlite;
using Duende.UserManagement;
using Duende.UserManagement.Authentication;
using Duende.UserManagement.Authentication.Passkeys;
using Microsoft.AspNetCore.DataProtection;

var builder = WebApplication.CreateBuilder(args);
var sampleOptions = builder.Configuration.GetSection(AccountLockoutSampleOptions.SectionName).Get<AccountLockoutSampleOptions>()
    ?? throw new InvalidOperationException($"Configuration section '{AccountLockoutSampleOptions.SectionName}' is required.");

var publicOrigin = new Uri(sampleOptions.PublicOrigin, UriKind.Absolute);
var databasePath = Path.GetFullPath(Path.Combine(builder.Environment.ContentRootPath, "..", "db", "account-lockout.db"));
Directory.CreateDirectory(Path.GetDirectoryName(databasePath)!);

// Service defaults connect logs, traces, metrics, and health checks to the Aspire dashboard.
builder.AddServiceDefaults();

builder.Services.AddRazorPages();
builder.Services.AddHttpContextAccessor();
builder.Services.AddSingleton<OtpCookie>();
builder.Services.AddSingleton(TimeProvider.System);

builder.Services.AddDataProtection()
    .SetApplicationName("UserManagement-AccountLockout-Sample");

builder.Services.AddOptions<AccountLockoutSampleOptions>()
    .Bind(builder.Configuration.GetSection(AccountLockoutSampleOptions.SectionName))
    .ValidateDataAnnotations()
    .ValidateOnStart();

builder.Services.AddScoped<SchemaBootstrapper>();
builder.Services.AddScoped<SampleDataSeeder>();

// Lockout is application policy over custom profile attributes, not built-in authenticator state.
builder.Services.AddScoped<AccountLockoutService>();
builder.Services.AddScoped<ApplicationSessionService>();

builder.Services.AddAuthorizationBuilder()
    .AddPolicy(Policies.Admin, policy =>
    {
        policy.RequireAuthenticatedUser();
        policy.RequireClaim(JwtClaimTypes.Subject, sampleOptions.AdminSubjectId);
    });

builder.Services
    .AddIdentityServer(options =>
    {
        options.UserInteraction.LoginUrl = "/Account/Login";
        options.UserInteraction.LogoutUrl = "/Account/Logout";
        options.Events.RaiseErrorEvents = true;
        options.Events.RaiseFailureEvents = true;
        options.Events.RaiseInformationEvents = true;
        options.Events.RaiseSuccessEvents = true;
    })
    .AddDeveloperSigningCredential(persistKey: false)
    .AddUserManagement(userManagement =>
    {
        userManagement.Authentication(authentication =>
        {
            authentication.Configure(options =>
            {
                options.Passkeys.ServerDomain = publicOrigin.Host;
                options.Passkeys.AllowedOrigins = [sampleOptions.PublicOrigin];
                options.Passkeys.RelyingPartyName = "Account Lockout Sample";
            });

            _ = authentication.UseSmtpOtpDispatcher(options => builder.Configuration.GetSection("Smtp").Bind(options));
        });

        userManagement.AddSqliteStore(options => options.ConnectionString = $"Data Source={databasePath}");
    });

// Replace the default passkey sign-in step so policy runs after WebAuthn verification,
// but before an authenticated session cookie is issued.
builder.Services.AddScoped<IPasskeySignInHandler, LockoutAwarePasskeySignInHandler>();

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;

    // A sample starts from an empty SQLite database, so create storage, schema, and users in order.
    await services.GetRequiredService<IDatabaseSchema>().MigrateAsync(CancellationToken.None);
    await services.GetRequiredService<SchemaBootstrapper>().BootstrapAsync(CancellationToken.None);
    await services.GetRequiredService<SampleDataSeeder>().SeedAsync(CancellationToken.None);
}

if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
}
else
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseRouting();
app.UseIdentityServer();
app.UseAuthorization();

app.MapStaticAssets();
app.MapRazorPages()
    .WithStaticAssets();

app.MapUserManagement();
app.MapDefaultEndpoints();

app.Run();
