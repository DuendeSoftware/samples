// Copyright (c) Duende Software. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using Duende.IdentityServer.Configuration;
using Duende.IdentityServer.Configuration.EntityFramework;
using Duende.IdentityServer.EntityFramework.DbContexts;
using Duende.IdentityServer.EntityFramework.Storage;
using Microsoft.EntityFrameworkCore;

Console.Title = "Configuration API";

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddIdentityServerConfiguration(opt => { })
    .AddClientConfigurationStore();

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
builder.Services.AddConfigurationDbContext<ConfigurationDbContext>(options =>
{
    options.ConfigureDbContext = b =>
        b.UseSqlite(connectionString, dbOpts => dbOpts.MigrationsAssembly(typeof(Program).Assembly.FullName));
});

builder.Services.AddAuthentication("token")
    .AddJwtBearer("token", options =>
    {
        options.Authority = "https://localhost:5001";
        options.MapInboundClaims = false;

        options.TokenValidationParameters.ValidateAudience = false;
        options.TokenValidationParameters.ValidTypes = new[] { "at+jwt" };
    });

builder.Services.AddAuthorization(opt =>
{
    opt.AddPolicy("DCR", policy =>
    {
        policy.RequireClaim("scope", "IdentityServer.Configuration");
    });
});

var app = builder.Build();
app.UseAuthentication();
app.UseAuthorization();
app.MapDynamicClientRegistration().RequireAuthorization("DCR");

app.Run();
