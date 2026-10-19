// Copyright (c) Duende Software. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using Duende.IdentityServer;
using Duende.IdentityServer.Models;
using Duende.IdentityServer.UserManagement;
using Duende.Storage.EntityAttributeValue;
using Duende.Storage.Schema;
using Duende.Storage.Sqlite;
using Duende.UserManagement;
using Duende.UserManagement.Authentication;
using Duende.UserManagement.Profiles;
using Duende.UserManagement.Scim;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.DataProtection;
using ScimProvisioning.IdentityServer;
using Secret = Duende.IdentityServer.Models.Secret;

var builder = WebApplication.CreateBuilder(args);
var issuer = builder.Configuration["Sample:IssuerUri"];
if (!Uri.TryCreate(issuer, UriKind.Absolute, out var issuerUri) || issuerUri.Scheme != Uri.UriSchemeHttps)
{
    throw new InvalidOperationException("Sample:IssuerUri must be an absolute HTTPS URL.");
}

builder.AddServiceDefaults();
builder.Services.AddRazorPages();
builder.Services.AddHttpContextAccessor();
builder.Services.AddSingleton<OtpCookie>();
builder.Services.AddDataProtection().SetApplicationName("ScimProvisioning");

var databasePath = Path.Combine(builder.Environment.ContentRootPath, "scim-provisioning.db");

#pragma warning disable duende_experimental
builder.Services.AddIdentityServer(options =>
    {
        options.IssuerUri = issuerUri.AbsoluteUri.TrimEnd('/');
        options.UserInteraction.LoginUrl = "/Account/Login";
        options.UserInteraction.LogoutUrl = "/Account/Logout";
    })
    .AddDeveloperSigningCredential(persistKey: false)
    .AddStorage(storage => storage.AddSqlite(options => options.ConnectionString = $"Data Source={databasePath}"))
    .AddInMemoryDataExtensionSchemas([
        new SchemaConfiguration
        {
            SchemaId = SchemaId.UserProfile,
            DisplayName = "SCIM User Profile",
            Groups = [.. DefaultScimUserSchema.Groups],
            AttributeDefinitions = [.. DefaultScimUserSchema.AttributeDefinitions]
        }
    ])
    .AddUserManagement(users =>
    {
        users.EnableScim(options => options.ChangePassword = false);
        users.Authentication(authentication =>
            authentication.UseSmtpOtpDispatcher(options => builder.Configuration.GetSection("Smtp").Bind(options)));
    })
    .AddInMemoryApiResources([ScimResources.ApiResource])
    .AddInMemoryApiScopes([ScimResources.FullAccessScope, ScimResources.ReadOnlyScope])
    .AddInMemoryClients([
        new Client
        {
            ClientId = "scim-provisioner",
            ClientSecrets = { new Secret("sample-secret".Sha256()) },
            AllowedGrantTypes = GrantTypes.ClientCredentials,
            AllowedScopes = { "scim" }
        },
        new Client
        {
            ClientId = "scim-reader",
            ClientSecrets = { new Secret("sample-reader-secret".Sha256()) },
            AllowedGrantTypes = GrantTypes.ClientCredentials,
            AllowedScopes = { "scim.read" }
        }
    ]);
#pragma warning restore duende_experimental

// Workaround for Duende.UserManagement 2.0.0-preview.6: the SCIM bearer scheme receives its
// Authority too late to build its metadata configuration, so valid tokens fail validation.
builder.Services.Configure<JwtBearerOptions>("DuendeScimBearer", options => options.Authority = issuerUri.AbsoluteUri.TrimEnd('/'));

var app = builder.Build();

await using (var scope = app.Services.CreateAsyncScope())
{
    await scope.ServiceProvider.GetRequiredService<IStorageInstanceSchema>()
        .MigrateAsync(app.Lifetime.ApplicationStopping);
}

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseRouting();
app.UseIdentityServer();
app.UseAuthorization();
app.MapRazorPages();
app.MapUserManagement();
#pragma warning disable duende_experimental
app.MapScim();
#pragma warning restore duende_experimental
app.MapDefaultEndpoints();

app.Run();
