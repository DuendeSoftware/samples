var builder = DistributedApplication.CreateBuilder(args);

var idsrv = builder.AddProject<Projects.IdentityServerHost>("identityserverhost");
builder.AddProject<Projects.Client>("client")
    .WaitFor(idsrv);

builder.Build().Run();
