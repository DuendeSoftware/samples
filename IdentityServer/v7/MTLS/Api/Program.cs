// Copyright (c) Duende Software. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using Api;
using Microsoft.AspNetCore.Server.Kestrel.Core;

Console.Title = "API";

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

builder.Services.AddControllers();

// this API will accept any access token from the authority
builder.Services.AddAuthentication("token")
    .AddJwtBearer("token", options =>
    {
        options.Authority = "https://localhost:5001";
        options.TokenValidationParameters.ValidateAudience = false;
        options.MapInboundClaims = false;

        options.TokenValidationParameters.ValidTypes = ["at+jwt"];
    });

builder.Services.Configure<KestrelServerOptions>(options =>
{
    options.ListenLocalhost(6001, config =>
    {
        config.UseHttps(https =>
        {
            https.ClientCertificateMode = Microsoft.AspNetCore.Server.Kestrel.Https.ClientCertificateMode.RequireCertificate;
            https.AllowAnyClientCertificate();
        });
    });
});

var app = builder.Build();

app.MapDefaultEndpoints();

app.UseRouting();

app.UseAuthentication();
app.UseConfirmationValidation();

app.UseAuthorization();

app.MapControllers().RequireAuthorization();

app.Run();
