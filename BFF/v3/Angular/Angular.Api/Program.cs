// Copyright (c) Duende Software. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using Angular.Api;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddAuthentication("token")
    .AddJwtBearer("token", options =>
    {
        options.Authority = "https://demo.duendesoftware.com";
        options.Audience = "api";

        options.MapInboundClaims = false;
    });

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("ApiCaller", policy =>
    {
        policy.RequireClaim("scope", "api");
    });

    options.AddPolicy("InteractiveUser", policy =>
    {
        policy.RequireClaim("sub");
    });
});

var app = builder.Build();

app.UseHttpsRedirection();

app.MapGroup("/todos")
    .ToDoGroup()
    .RequireAuthorization("ApiCaller", "InteractiveUser");


app.Run();

