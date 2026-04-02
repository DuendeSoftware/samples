var builder = DistributedApplication.CreateBuilder(args);

builder.AddProject<Projects.Api>("api");

builder.AddProject<Projects.Client>("client");

builder.AddProject<Projects.IdentityServerHost>("identityserverhost");

builder.Build().Run();
