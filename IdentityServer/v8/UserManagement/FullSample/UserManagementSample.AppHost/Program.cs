// Copyright (c) Duende Software. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

var builder = DistributedApplication.CreateBuilder(args);

var mailpit = builder.AddContainer("mailpit", "axllent/mailpit")
    .WithContainerName($"user-management-sample-mailpit")
    .WithHttpEndpoint(port: 8025, targetPort: 8025, name: "ui", isProxied: false)
    .WithEndpoint(port: 1025, targetPort: 1025, name: "smtp", isProxied: false);
var smtpEndpoint = mailpit.GetEndpoint("smtp");

var identityServer = builder.AddProject<Projects.UserManagementSample>("identity-server")
    .WaitFor(mailpit)
    .WithSmtp(smtpEndpoint);

builder.AddProject<Projects.UserManagementSample_Client>("client")
    .WithReference(identityServer);

builder.AddProject<Projects.UserManagementSample_AspNetIdentitySource>("aspnet-identity-source");

builder.Build().Run();

internal static class AspireExtensions
{
    /// <summary>
    /// Injects SMTP configuration (Smtp:Host, Smtp:Port, Smtp:EnableSsl) from a mailpit endpoint.
    /// </summary>
    internal static IResourceBuilder<T> WithSmtp<T>(this IResourceBuilder<T> project, EndpointReference smtpEndpoint)
        where T : IResourceWithEnvironment =>
        project
            .WithEnvironment("Smtp__Host", smtpEndpoint.Property(EndpointProperty.Host))
            .WithEnvironment("Smtp__Port", smtpEndpoint.Property(EndpointProperty.Port))
            .WithEnvironment("Smtp__FromEmail", "no-reply@localhost")
            .WithEnvironment("Smtp__FromName", "UserManagement Sample")
            .WithEnvironment("Smtp__EnableSsl", "false");
}
