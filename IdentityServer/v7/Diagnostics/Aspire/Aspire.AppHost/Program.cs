var builder = DistributedApplication.CreateBuilder(args);

var identityServer = builder.AddProject<Projects.IdentityServer>("identityserver")
    .WithExternalHttpEndpoints();

var apiService = builder.AddProject<Projects.Aspire_ApiService>("apiservice")
    .WithReference(identityServer);

var webFrontend = builder.AddProject<Projects.Aspire_Web>("webfrontend")
    .WithReference(apiService)
    .WithReference(identityServer)
    .WithExternalHttpEndpoints();

identityServer.WithReference(webFrontend);

builder.Build().Run();
