// Copyright (c) Duende Software. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using Duende.IdentityServer;
using Duende.IdentityServer.UserManagement;
using Duende.IdentityServer.Validation;
using Duende.Spaces;
using Duende.Storage.EntityAttributeValue;
using Duende.Storage.Schema;
using Duende.Storage.Sqlite;
using Duende.UserManagement;
using Duende.UserManagement.Profiles;
using Spaces.Seed;
using Spaces.Services;

var builder = WebApplication.CreateBuilder(args);
builder.Logging.SetMinimumLevel(LogLevel.Debug);

builder.Services.AddRazorPages();
builder.Services.AddHttpContextAccessor();

builder.Services.AddHttpClient("TokenRequest")
    .ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler
    {
        ServerCertificateCustomValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator
    });

builder.Services.AddSpaces();
builder.Services.Configure<SpacesOptions>(o => o.FallbackToDefault = true);

builder.Services
    .AddIdentityServer(options =>
    {
        options.UserInteraction.LoginUrl = "/Account/Login";
        options.UserInteraction.LogoutUrl = "/Account/Logout";
        options.Events.RaiseErrorEvents = true;
        options.Events.RaiseInformationEvents = true;
        options.Events.RaiseFailureEvents = true;
        options.Events.RaiseSuccessEvents = true;
    })
    .AddStorage(x => x.AddSqliteInMemory())
    .AddConfigurationStorage()
    .AddOperationalStorage()
    .AddUserManagement(um =>
    {
        um.Authentication(x => {  });
    })
    .AddProfileService<SpaceClaimAugmentationProfileService>()
    // The built-in user profile schema already contains email, name, given_name and
    // family_name. Extend it with the unique username used for password login.
    .AddInMemoryDataExtensionSchemas([BuiltInSchemas.UserProfile.Extend(DemoUserAttributes.UserName)]);

builder.Services.AddTransient<ICustomTokenRequestValidator, AddSpaceNameToClaimsRequestValidator>();
builder.Services.AddTransient<UserManagementProfileService>();


var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
}

using (var scope = app.Services.CreateScope())
{
    var sp = scope.ServiceProvider;

    await sp.GetRequiredService<IStorageInstanceSchema>().MigrateAsync(CancellationToken.None);

    await new SeedData(sp).Seed(CancellationToken.None);
}

app.UseHttpsRedirection();

// Space resolution must run before IdentityServer / auth so that the correct
// pool context is established before any IS middleware reads from storage.
app.UseSpaceResolution();
app.UseStaticFiles();
app.UseRouting();

app.UseIdentityServer();
app.UseAuthorization();

app.MapRazorPages();
app.MapUserManagement();

app.Run();

