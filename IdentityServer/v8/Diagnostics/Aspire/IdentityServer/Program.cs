using IdentityServer;

Console.WriteLine("Starting up");

try
{
    var builder = WebApplication.CreateBuilder(args);

    var app = builder
        .ConfigureServices()
        .ConfigurePipeline();
    
    app.Run();
}
catch (Exception ex)
{
    Console.WriteLine($"Unhandled exception {ex}");
}
finally
{
    Console.WriteLine("Shut down complete");
}