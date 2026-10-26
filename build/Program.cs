using System;
using System.IO;
using System.Linq;
using static Bullseye.Targets;
using static SimpleExec.Command;

namespace build
{
    internal static class Program
    {
        internal static void Main(string[] args)
        {
            Target("build", () =>
            {
                var files = Directory.GetFiles(".", "*.slnx", SearchOption.AllDirectories)
                    .Where(f => !f.StartsWith("./various", StringComparison.OrdinalIgnoreCase))
                    // The root aggregate solution includes the various/ projects and duplicates every other solution.
                    .Where(f => !string.Equals(f, Path.Combine(".", "samples.slnx"), StringComparison.OrdinalIgnoreCase));

                foreach (var file in files)
                {
                    Console.WriteLine($"building {file}");
                    Run("dotnet", $"build {file} -c Release --nologo");
                }
            });

            Target("default", DependsOn("build"));

            RunTargetsAndExit(args);
        }
    }
}