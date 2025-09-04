// Copyright (c) Duende Software. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

var builder = DistributedApplication.CreateBuilder(args);

var bff = builder.AddProject<Projects.bff>("bff");

var api = builder.AddProject<Projects.api>("api");

var customerPortal = builder.AddNpmApp("customer-portal", "../frontends/customer-portal", "dev")
    .WithEndpoint(5175, isProxied: false, scheme: "https"); ;
var managementApp = builder.AddNpmApp("management-app", "../frontends/management-app", "dev")
    .WithEndpoint(5173, isProxied: false, scheme: "https");

bff
    .WithReference(api)
    .WithReference(managementApp)
    .WithReference(customerPortal);

builder.Build().Run();
