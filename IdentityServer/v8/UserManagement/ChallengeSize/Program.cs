// Copyright (c) Duende Software. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using Duende.IdentityServer;
using Duende.Storage.Schema;
using Duende.Storage.Sqlite;
using Duende.UserManagement;
using Duende.UserManagement.Authentication;
using Microsoft.AspNetCore.DataProtection;

var builder = WebApplication.CreateBuilder(args);

builder.Services
    .AddRazorPages();

builder.Services
    .AddDataProtection()
    .SetApplicationName("UserManagement-ChallengeSize");

builder.Services
    .AddIdentityServer()
    .AddDeveloperSigningCredential(persistKey: false)
    .AddStorage(storage => storage.AddSqliteInMemory())
    .AddUserManagement(userManagement => userManagement.Authentication(authentication =>
        authentication.Configure(options =>
        {
            options.Passkeys.ServerDomain = "localhost";
            options.Passkeys.AllowedOrigins = ["https://localhost:5018"];
            options.Passkeys.RelyingPartyName = "Challenge Size Sample";

            // Gets the challenge size from configuration if it is set, otherwise uses the default value of 32 bytes.
            if (builder.Configuration.GetValue<int?>("Passkeys:ChallengeSize") is { } challengeSize)
            {
                options.Passkeys.ChallengeSize = challengeSize;
            }
        }))
    );

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
app.Run();
