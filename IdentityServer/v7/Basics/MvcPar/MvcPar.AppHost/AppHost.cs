var builder = DistributedApplication.CreateBuilder(args);

var idp = builder.AddProject<Projects.IdentityServerHost>("identityserverhost");

var api = builder.AddProject<Projects.SimpleApi>("simple-api");

builder.AddProject<Projects.Client>("client")
    .WaitFor(idp)
    .WaitFor(api);

builder.Build().Run();
