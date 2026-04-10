var builder = DistributedApplication.CreateBuilder(args);
var idsvr = builder.AddProject<Projects.IdentityServerHost>("identityserverhost");
builder.AddProject<Projects.Client>("client")
    .WaitFor(idsvr);
builder.Build().Run();
