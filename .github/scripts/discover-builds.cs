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
// When CHANGED_FILES names a file listing changed paths (one per line, relative to the
// repository root), only the samples affected by those changes are emitted:
// - a change under a sample's solution/project directories (including referenced projects
//   and files included from outside them) affects that sample
// - a change to a shared build input (global.json, Directory.Build.props/targets,
//   Directory.Packages.props, nuget.config, .editorconfig) affects every sample beneath it
// - a change under .github/ affects every sample
//
// Usage: dotnet run .github/scripts/discover-builds.cs

using System.Text.Json.Nodes;
using System.Text.RegularExpressions;

var root = FindRepositoryRoot(Environment.CurrentDirectory);
Directory.SetCurrentDirectory(root);

var files = EnumerateFiles(root).Select(f => Path.GetRelativePath(root, f).Replace('\\', '/')).Order(StringComparer.Ordinal).ToList();

var dotnet = new List<(JsonNode Entry, List<string> Roots)>();
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

    var roots = new HashSet<string>(StringComparer.Ordinal) { DirOf(solution) };
    foreach (var project in projects)
    {
        AddProjectRoots(project, roots);
    }

    dotnet.Add((DotnetEntry(solution, projects), roots.ToList()));
}

foreach (var project in files.Where(f => f.EndsWith(".csproj") && !covered.Contains(f)))
{
    var roots = new HashSet<string>(StringComparer.Ordinal);
    AddProjectRoots(project, roots);
    dotnet.Add((DotnetEntry(project, [project]), roots.ToList()));
}

var node = files
    .Where(f => Path.GetFileName(f) == "package-lock.json")
    .Select(f => DirOf(f))
    .Select(dir => (Entry: (JsonNode)new JsonObject { ["path"] = dir }, Roots: new List<string> { dir }))
    .ToList();

var changedFilesPath = Environment.GetEnvironmentVariable("CHANGED_FILES");
if (!string.IsNullOrEmpty(changedFilesPath))
{
    var changed = File.ReadAllLines(changedFilesPath)
        .Select(l => l.Trim().Replace('\\', '/'))
        .Where(l => l.Length > 0)
        .ToList();

    var all = dotnet.Count + node.Count;
    dotnet = dotnet.Where(e => IsAffected(e.Roots, changed)).ToList();
    node = node.Where(e => IsAffected(e.Roots, changed)).ToList();
    Console.Error.WriteLine($"{changed.Count} changed file(s); building {dotnet.Count + node.Count} of {all} sample(s).");
}

var output = $"dotnet={ToJson(dotnet)}\nnode={ToJson(node)}\n";
var githubOutput = Environment.GetEnvironmentVariable("GITHUB_OUTPUT");
if (string.IsNullOrEmpty(githubOutput))
{
    Console.Write(output);
}
else
{
    File.AppendAllText(githubOutput, output);
}

static string ToJson(IEnumerable<(JsonNode Entry, List<string> Roots)> entries) =>
    new JsonArray(entries.Select(e => e.Entry).ToArray()).ToJsonString();

// Directory of a repository-relative path ("" for the repository root).
static string DirOf(string path) => (Path.GetDirectoryName(path) ?? "").Replace('\\', '/');

static bool IsUnder(string path, string dir) => dir.Length == 0 || path == dir || path.StartsWith(dir + "/", StringComparison.Ordinal);

static bool IsAffected(List<string> roots, List<string> changed)
{
    string[] sharedInputs = ["global.json", "directory.build.props", "directory.build.targets", "directory.packages.props", "nuget.config", ".editorconfig"];

    foreach (var file in changed)
    {
        if (IsUnder(file, ".github"))
        {
            return true;
        }

        if (roots.Any(root => IsUnder(file, root)))
        {
            return true;
        }

        if (sharedInputs.Contains(Path.GetFileName(file).ToLowerInvariant()) && roots.Any(root => IsUnder(root, DirOf(file))))
        {
            return true;
        }
    }

    return false;
}

// Adds the project's directory plus any directories it pulls in from outside it
// (project references, imports and linked items), following project references transitively.
static void AddProjectRoots(string project, HashSet<string> roots, HashSet<string>? visited = null)
{
    visited ??= new HashSet<string>(StringComparer.Ordinal);
    roots.Add(DirOf(project));
    if (!visited.Add(project) || !File.Exists(project))
    {
        return;
    }

    var dir = Path.GetDirectoryName(project)!;
    foreach (Match match in Regex.Matches(File.ReadAllText(project), "(?:Include|Project)=\"([^\"]*\\.\\.[\\\\/][^\"]*)\""))
    {
        var value = match.Groups[1].Value.Replace('\\', '/');
        var wildcard = value.IndexOfAny(['*', '?', '$']);
        if (wildcard >= 0)
        {
            value = value[..value.LastIndexOf('/', wildcard)];
        }

        var full = Path.GetRelativePath(Environment.CurrentDirectory, Path.GetFullPath(Path.Combine(dir, value))).Replace('\\', '/');
        if (full.StartsWith("..", StringComparison.Ordinal))
        {
            continue;
        }

        if (full.EndsWith("proj", StringComparison.Ordinal))
        {
            AddProjectRoots(full, roots, visited);
        }
        else
        {
            roots.Add(Directory.Exists(full) ? full : DirOf(full));
        }
    }
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
