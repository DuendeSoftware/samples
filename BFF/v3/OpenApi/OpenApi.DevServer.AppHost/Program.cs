// Copyright (c) Duende Software. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

var builder = DistributedApplication.CreateBuilder(args);

builder.AddProject<Projects.OpenApi_Api1>("openapi-api1");

builder.AddProject<Projects.OpenApi_Api2>("openapi-api2");

builder.Build().Run();
