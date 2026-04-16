var builder = DistributedApplication.CreateBuilder(args);

builder.AddProject<Projects.SessionMigration>("sessionmigration");

builder.Build().Run();
