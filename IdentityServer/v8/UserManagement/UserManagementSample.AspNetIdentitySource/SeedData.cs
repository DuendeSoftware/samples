// Copyright (c) Duende Software. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using Microsoft.AspNetCore.Identity;
using System.Security.Claims;
using UserManagementSample.AspNetIdentitySource.Data;

namespace UserManagementSample.AspNetIdentitySource;

internal static class SeedData
{
    public static void EnsureSeedData(WebApplication app)
    {
        using var scope = app.Services.CreateScope();
        SeedAsync(scope.ServiceProvider).GetAwaiter().GetResult();
    }

    private static async Task SeedAsync(IServiceProvider services)
    {
        var userManager = services.GetRequiredService<UserManager<IdentityUser>>();

        await SeedUserAsync(userManager,
            email: "carol@example.com",
            password: "Password123!",
            claims:
            [
                new Claim("given_name", "Carol"),
                new Claim("family_name", "Smith"),
                new Claim("street_address", "One Carol Way"),
                new Claim("locality", "Carlington")
            ]);

        await SeedUserAsync(userManager,
            email: "dave@example.com",
            password: "Password456!",
            claims:
            [
                new Claim("given_name", "Dave"),
                new Claim("family_name", "Jones"),
                new Claim("street_address", "Two Dave Street"),
                new Claim("locality", "Daveville")
            ]);
    }

    private static async Task SeedUserAsync(
        UserManager<IdentityUser> userManager,
        string email,
        string password,
        IEnumerable<Claim> claims)
    {
        var existing = await userManager.FindByEmailAsync(email);
        if (existing is not null)
        {
            return;
        }

        var user = new IdentityUser
        {
            UserName = email,
            Email = email,
            EmailConfirmed = true
        };

        var result = await userManager.CreateAsync(user, password);
        if (!result.Succeeded)
        {
            throw new InvalidOperationException(
                $"Failed to create user '{email}': {string.Join(", ", result.Errors.Select(e => e.Description))}");
        }

        result = await userManager.AddClaimsAsync(user, claims);
        if (!result.Succeeded)
        {
            throw new InvalidOperationException(
                $"Failed to add claims to user '{email}': {string.Join(", ", result.Errors.Select(e => e.Description))}");
        }
    }
}
