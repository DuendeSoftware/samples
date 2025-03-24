// Copyright (c) Duende Software. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using Duende.AspNetCore.Authentication.JwtBearer.DPoP;
using Serilog;
using Serilog.Sinks.SystemConsole.Themes;

Console.Title = "API";

Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Information()
            .Enrich.FromLogContext()
            .WriteTo.Console(outputTemplate: "[{Timestamp:HH:mm:ss} {Level}] {SourceContext}{NewLine}{Message:lj}{NewLine}{Exception}{NewLine}", theme: AnsiConsoleTheme.Code)
            .CreateLogger();

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSerilog();
builder.Services.AddControllers();
builder.Services.AddCors();

// this API will accept any access token from the authority
builder.Services.AddAuthentication("token")
    .AddJwtBearer("token", options =>
    {
        options.Authority = "https://localhost:5001";
        options.TokenValidationParameters.ValidateAudience = false;
        options.MapInboundClaims = false;

        options.TokenValidationParameters.ValidTypes = ["at+jwt"];
    });

// layers DPoP onto the "token" scheme above
builder.Services.ConfigureDPoPTokensForScheme("token", opt =>
{
    // Chose a validation mode: either Nonce or IssuedAt. With nonce validation,
    // the api supplies a nonce that must be used to prove that the token was
    // not pre-generated. With IssuedAt validation, the client includes the
    // current time in the proof token, which is compared to the clock. Nonce
    // validation provides protection against some attacks that are possible
    // with IssuedAt validation, at the cost of an additional HTTP request being
    // required each time the API is invoked.
    //
    // See RFC 9449 for more details.
    opt.ValidationMode = ExpirationValidationMode.IssuedAt; // IssuedAt is the default.
});

var app = builder.Build();

app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers().RequireAuthorization();

app.Run();
