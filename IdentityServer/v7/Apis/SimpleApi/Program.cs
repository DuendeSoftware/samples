// Copyright (c) Duende Software. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
Console.Title = "Simple API";

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

builder.Services.AddControllers();

// Attention: This API will accept any access token from the authority in this configuration
builder.Services.AddAuthentication("token")
    .AddJwtBearer("token", options =>
    {
        options.Authority = "https://localhost:5001";
        options.TokenValidationParameters.ValidateAudience = false;

        options.TokenValidationParameters.ValidTypes = new[] { "at+jwt" };
        options.MapInboundClaims = false;
    });

// To require a scope, use a policy like this and apply it
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("SimpleApi", p => p.RequireClaim("scope", "SimpleApi"));
});

var app = builder.Build();

app.MapDefaultEndpoints();

app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers().RequireAuthorization();
//app.MapControllers().RequireAuthorization("SimpleApi");

app.Run();
