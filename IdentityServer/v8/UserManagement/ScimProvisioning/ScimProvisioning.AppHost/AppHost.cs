// Copyright (c) Duende Software. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

var builder = DistributedApplication.CreateBuilder(args);

var mailpit = builder.AddContainer("mailpit", "axllent/mailpit")
    .WithEndpoint(1028, 1025, "smtp", name: "smtp", isProxied: false)
    .WithEndpoint(8028, 8025, "http", name: "http", isProxied: false);

var smtp = mailpit.GetEndpoint("smtp");
var destination = builder.AddProject<Projects.ScimProvisioning_IdentityServer>("identityserver")
    .WaitForStart(mailpit)
    .WithEnvironment("Smtp__Host", smtp.Property(EndpointProperty.Host))
    .WithEnvironment("Smtp__Port", smtp.Property(EndpointProperty.Port))
    .WithEnvironment("Smtp__FromEmail", "scim-sample@example.com")
    .WithEnvironment("Smtp__FromName", "SCIM Provisioning Sample")
    .WithEnvironment("Smtp__EnableSsl", "false");

var authority = destination.GetEndpoint("https");
destination.WithEnvironment("Sample__IssuerUri", authority);

builder.AddProject<Projects.ScimProvisioning_Directory>("directory")
    .WithReference(destination)
    .WaitFor(destination)
    .WithEnvironment("Scim__Authority", authority)
    .WithEnvironment("Scim__BaseUrl", ReferenceExpression.Create($"{authority}/scim/"));

builder.Build().Run();
