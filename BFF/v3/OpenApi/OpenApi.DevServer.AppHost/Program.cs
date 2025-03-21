// Copyright (c) Duende Software. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using Microsoft.Extensions.Hosting;

var builder = DistributedApplication.CreateBuilder(args);

var api1 = builder.AddProject<Projects.OpenApi_Api1>(Services.Api1.ToString());

var api2 = builder.AddProject<Projects.OpenApi_Api2>(Services.Api2.ToString());
var bff = builder.AddProject<Projects.OpenApi_Bff>(Services.Bff.ToString());

bff.WithReference(api1)
    .WithReference(api2)
    .WithReference(bff)
    ;

builder.Build().Run();
