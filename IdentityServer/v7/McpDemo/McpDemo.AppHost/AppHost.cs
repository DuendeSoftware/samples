// Copyright (c) Duende Software. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

var builder = DistributedApplication.CreateBuilder(args);

var idp = builder.AddProject<Projects.McpDemo_IdentityServer>("mcpdemo-identityserver");

var mcpServer = builder.AddProject<Projects.McpDemo_McpServer>("mcpdemo-mcpserver")
    .WaitFor(idp);

builder.AddProject<Projects.McpDemo_Client>("mcpdemo-client")
    .WaitFor(idp)
    .WaitFor(mcpServer);

builder.Build().Run();
