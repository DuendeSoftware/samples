// Copyright (c) Duende Software. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

var builder = DistributedApplication.CreateBuilder(args);

var idp = builder.AddProject<Projects.IdentityServerHost>("identityserverhost");

var api = builder.AddProject<Projects.Api>("api");

var patParameter = builder.AddParameter("generated-pat");

var client = builder.AddProject<Projects.Client>("client")
    .WithArgs("--pat", patParameter);

builder.Build().Run();
