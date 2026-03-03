// Copyright (c) Duende Software. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using Api.Authorization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.IdentityModel.Tokens;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddAuthentication("jwt")
    .AddJwtBearer("jwt", opt =>
    {
        opt.Authority = "https://localhost:5001";
        opt.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateAudience = false,
            ValidTypes = new[] { "at+jwt" }
        };
        opt.MapInboundClaims = false;
    });

builder.Services.AddAuthorization(opt =>
{
    opt.AddPolicy("MaxAgeOneMinute", p =>
    {
        p.RequireAuthenticatedUser();
        p.AddRequirements(new MaxAgeRequirement(TimeSpan.FromMinutes(1)));
    });
    opt.AddPolicy("MfaRequired", p =>
    {
        p.RequireAuthenticatedUser();
        p.RequireClaim("amr", "mfa");
    });
    opt.AddPolicy("RecentMfa", p =>
    {
        p.RequireAuthenticatedUser();
        p.RequireClaim("amr", "mfa");
        p.AddRequirements(new MaxAgeRequirement(TimeSpan.FromSeconds(30)));
    });
});

builder.Services.AddSingleton<IAuthorizationHandler, MaxAgeHandler>();
builder.Services.AddSingleton<IAuthorizationMiddlewareResultHandler, StepUpAuthorizationMiddlewareResultHandler>();

builder.Services.AddControllers();

var app = builder.Build();

app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();
