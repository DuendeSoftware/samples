var builder = DistributedApplication.CreateBuilder(args);

var mailpit = builder.AddContainer("mailpit", "axllent/mailpit")
    .WithHttpEndpoint(port: 8025, targetPort: 8025, name: "ui")
    .WithEndpoint(port: 1025, targetPort: 1025, name: "smtp");

var smtpEndpoint = mailpit.GetEndpoint("smtp");

var identityServer = builder.AddProject<Projects.UserManagementSample>("identityserver")
    .WithEnvironment("ConnectionStrings__mailpit", smtpEndpoint)
    .WaitFor(mailpit);

builder.AddProject<Projects.UserManagementSample_Client>("client")
    .WithReference(identityServer);

builder.AddProject<Projects.UserManagementSample_AspNetIdentitySource>("aspnetidentitysource");

builder.Build().Run();
