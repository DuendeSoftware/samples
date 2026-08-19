var builder = DistributedApplication.CreateBuilder(args);

var idp = builder.AddProject<Projects.IdentityServerHost>("identityserverhost");

var api = builder.AddProject<Projects.Api>("api");

var patParameter = builder.AddParameter("generated-pat");

var client = builder.AddProject<Projects.Client>("client")
    .WithArgs("--pat", patParameter);

builder.Build().Run();
