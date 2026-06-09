var builder = DistributedApplication.CreateBuilder(args);

var mailpit = builder.AddContainer("mailpit", "axllent/mailpit")
    .WithHttpEndpoint(port: 8025, targetPort: 8025, name: "ui", isProxied: false)
    .WithEndpoint(port: 1025, targetPort: 1025, name: "smtp", isProxied: false);

var identityServer = builder.AddProject<Projects.UserManagementSample>("identityserver")
    .WithEnvironment("ConnectionStrings__mailpit", mailpit.GetEndpoint("smtp"))
    .WaitFor(mailpit);

builder.AddProject<Projects.UserManagementSample_Client>("client")
    .WithReference(identityServer);

builder.AddProject<Projects.UserManagementSample_AspNetIdentitySource>("aspnetidentitysource");

builder.AddProject<Projects.UserManagementSample>("sample");
builder.AddProject<Projects.UserManagementSample_GettingStarted>("gettingstarted");

builder.Build().Run();
