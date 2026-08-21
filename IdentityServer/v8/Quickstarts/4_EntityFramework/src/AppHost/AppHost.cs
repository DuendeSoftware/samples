// Copyright (c) Duende Software. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

var builder = DistributedApplication.CreateBuilder(args);

var api = builder.AddProject<Projects.Api>("api");
var webClient = builder.AddProject<Projects.WebClient>("web-client");
var idp = builder.AddProject<Projects.IdentityServer>("identityserver");

builder.AddProject<Projects.Client>("client")
    .WaitFor(api)
    .WaitFor(webClient)
    .WaitFor(idp);

builder.Build().Run();
