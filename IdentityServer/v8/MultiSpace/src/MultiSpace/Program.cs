// Copyright (c) Duende Software. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using Duende.IdentityServer;
using Duende.IdentityServer.UserManagement;
using Duende.IdentityServer.Validation;
using Duende.MultiSpace;
using Duende.Storage.Schema;
using Duende.Storage.Sqlite;
using Duende.UserManagement;
using MultiSpace.Seed;
using MultiSpace.Services;

var builder = WebApplication.CreateBuilder(args);
builder.Logging.SetMinimumLevel(LogLevel.Debug);

builder.Services.AddRazorPages();
builder.Services.AddHttpContextAccessor();

builder.Services.AddHttpClient("TokenRequest")
    .ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler
    {
        ServerCertificateCustomValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator
    });

builder.Services.AddMultiSpace();
builder.Services.Configure<MultiSpaceOptions>(o => o.FallbackToDefault = true);

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
    .AddUserManagement(um =>
    {
        um.Authentication(x => {  });

        um.AddSqliteInMemoryStore();
    })
    .AddProfileService<SpaceClaimAugmentationProfileService>()
    .AddConfigurationStorage();

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

    await sp.GetRequiredService<IDatabaseSchema>().MigrateAsync(CancellationToken.None);

    await new SeedData(sp).Seed(CancellationToken.None);
}

app.UseHttpsRedirection();

// Space resolution must run before IdentityServer / auth so that the correct
// pool context is established before any IS middleware reads from storage.
app.UseMultiSpaceResolution();
app.UseStaticFiles();
app.UseRouting();

app.UseIdentityServer();
app.UseAuthorization();

app.MapRazorPages();
app.MapUserManagement();

app.Run();

