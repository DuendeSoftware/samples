var builder = DistributedApplication.CreateBuilder(args);

builder.AddProject<Projects.MultiSpace>("multispace")
    .WithEndpoint("https", e =>
    {
        e.Port = 5000;
        e.TargetPort = 5000;
        e.IsProxied = false;
    });

builder.Build().Run();
