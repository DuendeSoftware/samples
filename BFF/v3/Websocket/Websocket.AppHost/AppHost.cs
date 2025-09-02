// Copyright (c) Duende Software. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using Projects;

var builder = DistributedApplication.CreateBuilder(args);

builder.AddProject<Websocket_Bff>("bff");
builder.AddProject<Websocket_GraphQLServer>("graphql");

builder.Build().Run();
