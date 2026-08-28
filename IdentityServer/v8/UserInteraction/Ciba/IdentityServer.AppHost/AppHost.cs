// Copyright (c) Duende Software. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

var builder = DistributedApplication.CreateBuilder(args);

var idsvr = builder.AddProject<Projects.IdentityServerHost>("identityserverhost");

var api = builder.AddProject<Projects.SimpleApi>("simpleapi");

builder.AddProject<Projects.Client>("client")
    .WaitFor(idsvr)
    .WaitFor(api)
    .WithEnvironment("IS_IN_ASPIRE", true.ToString());

builder.Build().Run();
