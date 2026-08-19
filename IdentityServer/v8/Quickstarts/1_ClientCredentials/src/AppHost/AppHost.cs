var builder = DistributedApplication.CreateBuilder(args);

var api = builder.AddProject<Projects.Api>("api");
var idp = builder.AddProject<Projects.IdentityServer>("identityserver");

builder.AddProject<Projects.Client>("client")
    .WaitFor(api)
    .WaitFor(idp);

builder.Build().Run();
