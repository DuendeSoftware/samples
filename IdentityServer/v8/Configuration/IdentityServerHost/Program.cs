// Copyright (c) Duende Software. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.Globalization;
using IdentityServer;
using Microsoft.Extensions.Hosting;

Console.Title = "IdentityServer Host";

try
{
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
}
catch (Exception ex) when (
                            // https://github.com/dotnet/runtime/issues/60600
                            ex.GetType().Name is not "StopTheHostException"
                            // HostAbortedException was added in .NET 7, but since we target .NET 6 we
                            // need to do it this way until we target .NET 8
                            && ex.GetType().Name is not "HostAbortedException"
                        )
{
    Console.WriteLine("Unhandled exception");
    Console.WriteLine(ex);
}
finally
{
    Console.WriteLine("Shut down complete");
}
