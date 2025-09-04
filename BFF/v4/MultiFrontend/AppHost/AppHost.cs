// Copyright (c) Duende Software. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

var builder = DistributedApplication.CreateBuilder(args);

builder.AddProject<Projects.bff>("bff");

builder.AddProject<Projects.api>("api");

builder.AddNpmApp("customer-portal", "../frontends/customer-portal", "dev")
    .WithEndpoint(5175, isProxied: false, scheme: "http"); ;
builder.AddNpmApp("management-app", "../frontends/management-app", "dev")
    .WithEndpoint(5173, isProxied: false, scheme: "http");

builder.Build().Run();
