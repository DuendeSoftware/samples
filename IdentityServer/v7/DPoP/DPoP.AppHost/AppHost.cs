// Copyright (c) Duende Software. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

var builder = DistributedApplication.CreateBuilder(args);

var idp = builder.AddProject<Projects.IdentityServerHost>("identityserverhost");

var api = builder.AddProject<Projects.Api>("api");

builder.AddProject<Projects.WebClient>("webclient")
    .WaitFor(idp)
    .WaitFor(api);

builder.AddProject<Projects.ClientCredentials>("clientcredentials")
    .WaitFor(idp)
    .WaitFor(api);

builder.Build().Run();
