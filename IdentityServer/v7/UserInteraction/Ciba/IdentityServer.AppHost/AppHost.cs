var builder = DistributedApplication.CreateBuilder(args);

var idsvr = builder.AddProject<Projects.IdentityServerHost>("identityserverhost");

var api = builder.AddProject<Projects.SimpleApi>("simpleapi");

builder.AddProject<Projects.Client>("client")
    .WaitFor(idsvr)
    .WaitFor(api);

builder.Build().Run();
