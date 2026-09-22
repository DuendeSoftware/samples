// Copyright (c) Duende Software. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

var builder = DistributedApplication.CreateBuilder(args);

var mailpit = builder.AddContainer("mailpit", "axllent/mailpit")
    .WithContainerName("account-lockout-mailpit")
    .WithEndpoint(1027, 1025, "smtp", name: "mailpit-smtp", isProxied: false)
    .WithEndpoint(8027, 8025, "http", name: "mailpit-http", isProxied: false);

var smtpEndpoint = mailpit.GetEndpoint("mailpit-smtp");

_ = builder.AddProject<Projects.AccountLockout>("account-lockout")
    .WaitForStart(mailpit)
    .WithSmtp(smtpEndpoint);

builder
    .Build()
    .Run();

internal static class AspireExtensions
{
    internal static IResourceBuilder<T> WithSmtp<T>(this IResourceBuilder<T> project, EndpointReference smtpEndpoint)
        where T : IResourceWithEnvironment =>
        project
            .WithEnvironment("Smtp__Host", smtpEndpoint.Property(EndpointProperty.Host))
            .WithEnvironment("Smtp__Port", smtpEndpoint.Property(EndpointProperty.Port))
            .WithEnvironment("Smtp__FromEmail", "account-lockout-sample@duendesoftware.com")
            .WithEnvironment("Smtp__FromName", "Account Lockout Sample")
            .WithEnvironment("Smtp__EnableSsl", "false");
}
