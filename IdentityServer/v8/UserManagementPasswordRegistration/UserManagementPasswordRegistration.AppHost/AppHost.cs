// Copyright (c) Duende Software. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

var builder = DistributedApplication.CreateBuilder(args);

var mailpit = builder.AddContainer("mailpit", "axllent/mailpit")
    .WithContainerName($"password-registration-mailpit")
    .WithEndpoint(1026, 1025, "smtp", name: "mailpit-smtp", isProxied: false)
    .WithEndpoint(8026, 8025, "http", name: "mailpit-http", isProxied: false);
var smtpEndpoint = mailpit.GetEndpoint("mailpit-smtp");

_ = builder.AddProject<Projects.UserManagementPasswordRegistration>("password-registration")
    .WaitForStart(mailpit)
    .WithSmtp(smtpEndpoint);

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
            .WithEnvironment("Smtp__FromEmail", "noreply@example.com")
            .WithEnvironment("Smtp__FromName", "noreply")
            .WithEnvironment("Smtp__EnableSsl", "false");
}
