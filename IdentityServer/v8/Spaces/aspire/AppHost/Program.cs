var builder = DistributedApplication.CreateBuilder(args);

builder.AddProject<Projects.Spaces>("Spaces")
    .WithEndpoint("https", e =>
    {
        e.Port = 5001;
        e.TargetPort = 5001;
        e.IsProxied = false;
    });

builder.Build().Run();
