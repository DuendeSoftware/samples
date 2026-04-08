var builder = DistributedApplication.CreateBuilder(args);

builder.AddProject<Projects.SimpleApi>("api");

var idsvr = builder.AddProject<Projects.IdentityServerHost>("identityserverhost");

builder.AddProject<Projects.Client>("client")
    .WaitFor(idsvr);

builder.Build().Run();
