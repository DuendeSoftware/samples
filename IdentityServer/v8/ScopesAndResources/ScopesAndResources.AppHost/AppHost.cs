// Copyright (c) Duende Software. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

var builder = DistributedApplication.CreateBuilder(args);

var clientResourceParameter = builder.AddParameter("resource", "help");

var idp = builder.AddProject<Projects.IdentityServerHost>("identityserver");

var client = builder.AddProject<Projects.Client>("client")
    .WithArgs("--resource", clientResourceParameter);

builder.Build().Run();
