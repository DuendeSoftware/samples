var builder = DistributedApplication.CreateBuilder(args);

var idp = builder.AddProject<Projects.McpDemo_IdentityServer>("mcpdemo-identityserver");

var mcpServer = builder.AddProject<Projects.McpDemo_McpServer>("mcpdemo-mcpserver")
    .WaitFor(idp);

builder.AddProject<Projects.McpDemo_Client>("mcpdemo-client")
    .WaitFor(idp)
    .WaitFor(mcpServer);

builder.Build().Run();
