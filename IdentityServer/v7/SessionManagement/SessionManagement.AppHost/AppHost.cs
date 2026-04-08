var builder = DistributedApplication.CreateBuilder(args);

builder.AddProject<Projects.SimpleApi>("api");

var idsrv = builder.AddProject<Projects.IdentityServerHost>("identityserverhost");

builder.AddProject<Projects.Client>("client")
    .WaitFor(idsrv);

builder.Build().Run();
