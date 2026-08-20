// Copyright (c) Duende Software. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

var builder = DistributedApplication.CreateBuilder(args);

//Only start the BFF project. The BlazorWasm project is hosted by the BFF project
builder.AddProject<Projects.Bff>("bff");

builder.Build().Run();
