var builder = DistributedApplication.CreateBuilder(args);

var idp = builder.AddProject<Projects.IdentityServerHost>("identityserverhost");

builder.AddProject<Projects.ResourceBasedApi>("resource-based-api")
    .WaitFor(idp);

builder.AddProject<Projects.Client>("client")
    .WaitFor(idp);

builder.Build().Run();
