var builder = DistributedApplication.CreateBuilder(args);

var idp = builder.AddProject<Projects.IdentityServerHost>("identityserverhost");

builder.AddProject<Projects.SimpleApi>("simple-api")
    .WaitFor(idp);

builder.AddProject<Projects.Client>("client")
    .WaitFor(idp);

builder.Build().Run();
