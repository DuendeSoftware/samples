// Discovers every buildable sample in the repository and emits GitHub Actions matrices.
//
// - every .sln/.slnx (except the root samples.slnx)
// - every .csproj not referenced by one of those solutions
// - every npm project with a package-lock.json
//
// Outputs (appended to $GITHUB_OUTPUT when set, otherwise written to stdout):
//   dotnet=[{"path": "...", "os": "...", "kind": "sdk|maui|netfx", "framework": "..."}, ...]
//   node=[{"path": "..."}, ...]
//
// Usage: dotnet run .github/scripts/discover-builds.cs

using System.Text.Json.Nodes;
using System.Text.RegularExpressions;

var root = FindRepositoryRoot(Environment.CurrentDirectory);
Directory.SetCurrentDirectory(root);

var files = EnumerateFiles(root).Select(f => Path.GetRelativePath(root, f).Replace('\\', '/')).Order(StringComparer.Ordinal).ToList();

var dotnet = new JsonArray();
var covered = new HashSet<string>(StringComparer.Ordinal);

foreach (var solution in files.Where(f => f is not "samples.slnx" && (f.EndsWith(".sln") || f.EndsWith(".slnx"))))
{
    var projects = new List<string>();
    foreach (var project in SolutionProjects(solution))
    {
        covered.Add(project);
        if (File.Exists(project))
        {
            projects.Add(project);
        }
    }

    if (projects.Count == 0)
    {
        // Broken references: classify by the projects next to the solution instead.
        var dir = Path.GetDirectoryName(solution)!;
        projects.AddRange(Directory.EnumerateFiles(dir, "*.csproj", SearchOption.AllDirectories));
    }

    dotnet.Add(DotnetEntry(solution, projects));
}

foreach (var project in files.Where(f => f.EndsWith(".csproj") && !covered.Contains(f)))
{
    dotnet.Add(DotnetEntry(project, [project]));
}

var node = new JsonArray(files
    .Where(f => Path.GetFileName(f) == "package-lock.json")
    .Select(f => (JsonNode)new JsonObject { ["path"] = Path.GetDirectoryName(f)!.Replace('\\', '/') })
    .ToArray());

var output = $"dotnet={dotnet.ToJsonString()}\nnode={node.ToJsonString()}\n";
var githubOutput = Environment.GetEnvironmentVariable("GITHUB_OUTPUT");
if (string.IsNullOrEmpty(githubOutput))
{
    Console.Write(output);
}
else
{
    File.AppendAllText(githubOutput, output);
}

static string FindRepositoryRoot(string start)
{
    for (var dir = new DirectoryInfo(start); dir is not null; dir = dir.Parent)
    {
        // .git is a directory in a normal clone and a file in a worktree.
        if (Directory.Exists(Path.Combine(dir.FullName, ".git")) || File.Exists(Path.Combine(dir.FullName, ".git")))
        {
            return dir.FullName;
        }
    }

    throw new InvalidOperationException($"No git repository found above '{start}'.");
}

static IEnumerable<string> EnumerateFiles(string root)
{
    var pending = new Stack<string>([root]);
    while (pending.TryPop(out var dir))
    {
        foreach (var file in Directory.EnumerateFiles(dir))
        {
            yield return file;
        }

        foreach (var child in Directory.EnumerateDirectories(dir))
        {
            var name = Path.GetFileName(child);
            var skip = name is ".git" or "node_modules" or "bin" or "obj"
                || (name == "build" && Path.GetDirectoryName(child) == root);
            if (!skip)
            {
                pending.Push(child);
            }
        }
    }
}

// Returns the project paths referenced by a solution, relative to the repository root.
static IEnumerable<string> SolutionProjects(string solution)
{
    var pattern = solution.EndsWith(".slnx") ? "Path=\"([^\"]+proj)\"" : "\"([^\"]+proj)\"";
    var dir = Path.GetDirectoryName(solution)!;
    foreach (Match match in Regex.Matches(File.ReadAllText(solution), pattern))
    {
        var full = Path.GetFullPath(Path.Combine(dir, match.Groups[1].Value.Replace('\\', '/')));
        yield return Path.GetRelativePath(Environment.CurrentDirectory, full).Replace('\\', '/');
    }
}

static JsonNode DotnetEntry(string path, IReadOnlyCollection<string> projects)
{
    var contents = projects.Select(File.ReadAllText).ToList();
    string kind = "sdk", os = "ubuntu-latest", framework = "";

    if (contents.Any(c => c.Contains("<UseMaui>true</UseMaui>")))
    {
        // iOS/MacCatalyst need a Mac; build the Windows head only.
        kind = "maui";
        os = "windows-latest";
        framework = contents
            .Select(c => Regex.Match(c, @"net[0-9.]*-windows[0-9.]*"))
            .FirstOrDefault(m => m.Success)?.Value ?? "";
    }
    else if (contents.Any(c => c.Contains("ToolsVersion=")))
    {
        // Old-style (non-SDK) projects target .NET Framework and need MSBuild + NuGet on Windows.
        kind = "netfx";
        os = "windows-latest";
    }

    return new JsonObject
    {
        ["path"] = path,
        ["os"] = os,
        ["kind"] = kind,
        ["framework"] = framework,
    };
}
