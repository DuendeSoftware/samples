var builder = DistributedApplication.CreateBuilder(args);

var idp = builder.AddProject<Projects.IdentityServerHost>("identityserverhost");

builder.AddProject<Projects.Client>("client")
    .WaitFor(idp);

idp.WithCommand(
    name: "seed",
    displayName: "Seed Database",
    executeCommand: async (context) =>
    {
        var projectMetadata = idp.Resource.GetProjectMetadata();
        var projectPath = projectMetadata.ProjectPath;
        var process = new System.Diagnostics.Process
        {
            StartInfo = new System.Diagnostics.ProcessStartInfo
            {
                FileName = "dotnet",
                Arguments = $"run --project \"{projectPath}\" --no-build -- /seed",
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true
            }
        };

        process.Start();
        await process.WaitForExitAsync(context.CancellationToken);

        if (process.ExitCode == 0)
        {
            return CommandResults.Success();
        }
        else
        {
            var error = await process.StandardError.ReadToEndAsync();
            return CommandResults.Failure(error);
        }
    },
    commandOptions: new CommandOptions
    {
        IconName = "DatabaseArrowUp",
        IsHighlighted = true
    });

builder.Build().Run();
