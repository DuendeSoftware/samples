// Copyright (c) Duende Software. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using UserManagementSample.AspNetIdentitySource;
using UserManagementSample.AspNetIdentitySource.Data;

var builder = WebApplication.CreateBuilder(args);

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
                       ?? $"Data Source={Path.Combine(builder.Environment.ContentRootPath, "..", "db", "aspnetidentity.db")}";

builder.Services
    .AddDbContext<ApplicationDbContext>(options =>
        options.UseSqlite(connectionString))
    .AddIdentityCore<IdentityUser>(options =>
    {
        options.Password.RequireDigit = true;
        options.Password.RequiredLength = 8;
        options.Password.RequireNonAlphanumeric = true;
        options.Password.RequireUppercase = true;
        options.Password.RequireLowercase = true;
    })
    .AddEntityFrameworkStores<ApplicationDbContext>();

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    await db.Database.MigrateAsync();
}

SeedData.EnsureSeedData(app);

app.MapGet("/health", () => Results.Ok("Healthy"));

await app.RunAsync();
