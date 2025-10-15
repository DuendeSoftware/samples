// Copyright (c) Duende Software. All rights reserved.
// See LICENSE in the project root for license information.

using MultiFrontendSSO.IdentityServer;
using Serilog;

Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .CreateBootstrapLogger();

Log.Information("Starting up");

try
{
    var webApplicationOptions = new WebApplicationOptions
    {
        Args = args,
        WebRootPath = "idsrv-wwwroot"
    };

    var builder = WebApplication.CreateBuilder(webApplicationOptions);

    builder.Host.UseSerilog(
        (ctx, lc) => lc
            .WriteTo.Console(
                outputTemplate:
                "[{Timestamp:HH:mm:ss} {Level}] {SourceContext}{NewLine}{Message:lj}{NewLine}{Exception}{NewLine}")
            .Enrich.FromLogContext()
            .ReadFrom.Configuration(ctx.Configuration));

    var app = builder
        .ConfigureServices()
        .ConfigurePipeline();

    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Unhandled exception");
}
finally
{
    Log.Information("Shut down complete");
    Log.CloseAndFlush();
}
