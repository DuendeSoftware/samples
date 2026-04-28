// Copyright (c) Duende Software. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.Globalization;
using IdentityServer;
using Microsoft.Extensions.Hosting;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

var app = builder
    .ConfigureServices()
    .ConfigurePipeline();

// this seeding is only for the template to bootstrap the DB and users.
// in production you will likely want a different approach.
if (args.Contains("/seed"))
{
    Console.WriteLine("Seeding database...");
    SeedData.EnsureSeedData(app);
    Console.WriteLine("Done seeding database. Exiting.");
    return;
}

app.Run();
