var builder = DistributedApplication.CreateBuilder(args);
var idsvr = builder.AddProject<Projects.IdentityServerHost>("identityserverhost");

idsvr.WithCommand(
    name: "seed",
    displayName: "Seed Database",
    executeCommand: async (context) =>
    {
        var projectMetadata = idsvr.Resource.GetProjectMetadata();
        var projectPath = projectMetadata.ProjectPath;

        using var process = new System.Diagnostics.Process
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

        var outputTask = process.StandardOutput.ReadToEndAsync();
        var errorTask = process.StandardError.ReadToEndAsync();

        try
        {
            await process.WaitForExitAsync(context.CancellationToken);
        }
        catch (OperationCanceledException)
        {
            process.Kill(entireProcessTree: true);
            throw;
        }

        if (process.ExitCode == 0)
        {
            return CommandResults.Success();
        }
        else
        {
            var error = await errorTask;
            if (string.IsNullOrWhiteSpace(error))
            {
                error = await outputTask;
            }

            return CommandResults.Failure(error);
        }
    },
    commandOptions: new CommandOptions
    {
        IconName = "DatabaseArrowUp",
        IsHighlighted = true
    });

builder.AddProject<Projects.Client>("client")
    .WaitFor(idsvr);
builder.Build().Run();
