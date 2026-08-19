var builder = DistributedApplication.CreateBuilder(args);

var api = builder.AddProject<Projects.Api>("api");
var webClient = builder.AddProject<Projects.WebClient>("web-client");
var idp = builder.AddProject<Projects.IdentityServer>("identityserver");

builder.AddProject<Projects.Client>("client")
    .WaitFor(api)
    .WaitFor(webClient)
    .WaitFor(idp);

builder.Build().Run();
