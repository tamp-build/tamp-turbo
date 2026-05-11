using System.IO;
using System.Text.Json;
using Tamp;
using Xunit;
using Xunit.Abstractions;

namespace Tamp.Turbo.V2.IntegrationTests;

/// <summary>
/// Exercises the wrapper against a real Turborepo 2.x install. Stages
/// a tiny monorepo per fixture (root package.json with workspaces +
/// turbo.json + two child packages) so all the verbs have something
/// real to chew on.
/// </summary>
public sealed class TurboIntegrationTests : IDisposable
{
    private readonly ITestOutputHelper _output;
    private readonly AbsolutePath _workdir;

    public TurboIntegrationTests(ITestOutputHelper output)
    {
        _output = output;
        _workdir = AbsolutePath.Create(Path.Combine(Path.GetTempPath(), $"tamp-turbo-it-{Guid.NewGuid():N}"));
        Directory.CreateDirectory(_workdir.Value);

        // Root package.json with workspaces + scripts referenced from turbo.json.
        // packageManager is required by Turbo 2 to identify which PM runs the
        // workspace; we use npm here since it ships with Node and the
        // tests don't actually need yarn berry semantics.
        File.WriteAllText(Path.Combine(_workdir.Value, "package.json"), """
            {
              "name": "tamp-turbo-smoke",
              "private": true,
              "packageManager": "npm@10.0.0",
              "workspaces": ["packages/*"]
            }
            """);

        // Minimal turbo.json (Turbo 2.x schema)
        File.WriteAllText(Path.Combine(_workdir.Value, "turbo.json"), """
            {
              "$schema": "https://turborepo.com/schema.json",
              "tasks": {
                "echo": { "outputs": [] },
                "build": { "outputs": ["dist/**"] }
              }
            }
            """);

        // Two child packages so workspace filtering has something to do.
        foreach (var name in new[] { "a", "b" })
        {
            var pkgDir = Path.Combine(_workdir.Value, "packages", name);
            Directory.CreateDirectory(pkgDir);
            File.WriteAllText(Path.Combine(pkgDir, "package.json"), $$"""
                {
                  "name": "@smoke/{{name}}",
                  "version": "0.0.0",
                  "scripts": {
                    "echo": "node -e \"console.log('hi from {{name}}')\"",
                    "build": "node -e \"console.log('built {{name}}')\""
                  }
                }
                """);
        }

        // turbo prune --docker requires a real lockfile (HOL-49 nuance).
        // Run npm install --package-lock-only to generate one without
        // actually fetching anything to node_modules.
        var npmInstall = new System.Diagnostics.ProcessStartInfo("npm")
        {
            WorkingDirectory = _workdir.Value,
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
        };
        npmInstall.ArgumentList.Add("install");
        npmInstall.ArgumentList.Add("--package-lock-only");
        npmInstall.ArgumentList.Add("--silent");
        using var p = System.Diagnostics.Process.Start(npmInstall);
        p?.WaitForExit();
    }

    private static Tool ResolveTool()
    {
        var pathEnv = Environment.GetEnvironmentVariable("PATH") ?? "";
        var names = OperatingSystem.IsWindows()
            ? new[] { "turbo.cmd", "turbo.exe", "turbo.bat", "turbo.ps1", "turbo" }
            : new[] { "turbo" };
        foreach (var dir in pathEnv.Split(Path.PathSeparator))
        {
            if (string.IsNullOrEmpty(dir)) continue;
            foreach (var n in names)
            {
                var c = Path.Combine(dir, n);
                if (File.Exists(c)) return new Tool(AbsolutePath.Create(c));
            }
        }
        throw new InvalidOperationException("turbo not found on PATH. Install: npm i -g turbo");
    }

    private CaptureResult Run(CommandPlan plan)
    {
        _output.WriteLine($"$ {plan.Executable} {string.Join(' ', plan.Arguments)}");
        var result = ProcessRunner.Capture(plan);
        foreach (var line in result.Lines)
            _output.WriteLine($"  [{line.Type}] {line.Text}");
        _output.WriteLine($"  → exit {result.ExitCode}");
        return result;
    }

    [Fact]
    public void Run_Echo_Across_Workspaces_Succeeds()
    {
        var tool = ResolveTool();
        var plan = Turbo.Run(tool, s => s
            .AddTask("echo")
            .SetCwd(_workdir.Value)
            .SetWorkingDirectory(_workdir.Value));
        var result = Run(plan);
        Assert.Equal(0, result.ExitCode);
        // Each workspace's echo runs.
        var combined = result.StdoutText;
        Assert.Contains("hi from a", combined);
        Assert.Contains("hi from b", combined);
    }

    [Fact]
    public void Run_Filter_Limits_To_One_Workspace()
    {
        var tool = ResolveTool();
        var plan = Turbo.Run(tool, s => s
            .AddTask("echo")
            .AddFilter("@smoke/a")
            .SetCwd(_workdir.Value)
            .SetWorkingDirectory(_workdir.Value));
        var result = Run(plan);
        Assert.Equal(0, result.ExitCode);
        Assert.Contains("hi from a", result.StdoutText);
        Assert.DoesNotContain("hi from b", result.StdoutText);
    }

    [Fact]
    public void Run_DryRun_Json_Returns_Plan_Without_Executing()
    {
        var tool = ResolveTool();
        var plan = Turbo.Run(tool, s => s
            .AddTask("build")
            .SetDryRun("json")
            .SetCwd(_workdir.Value)
            .SetWorkingDirectory(_workdir.Value));
        var result = Run(plan);
        Assert.Equal(0, result.ExitCode);
        // The JSON plan dump contains the command strings (so we can't
        // textually grep for "built a"). Instead verify the plan has
        // the expected shape AND no dist/ directory got created (the
        // build script's would-be side-effect).
        using var doc = JsonDocument.Parse(result.StdoutText);
        Assert.True(doc.RootElement.TryGetProperty("tasks", out var tasks));
        Assert.True(tasks.GetArrayLength() >= 1);
        Assert.False(Directory.Exists(Path.Combine(_workdir.Value, "packages", "a", "dist")));
    }

    [Fact]
    public void Prune_Docker_Generates_Out_Directory()
    {
        var tool = ResolveTool();
        var plan = Turbo.Prune(tool, s => s
            .AddScope("@smoke/a")
            .SetDocker()
            .SetOutDir("pruned")
            .SetCwd(_workdir.Value)
            .SetWorkingDirectory(_workdir.Value));
        var result = Run(plan);
        Assert.Equal(0, result.ExitCode);
        var outDir = Path.Combine(_workdir.Value, "pruned");
        Assert.True(Directory.Exists(outDir), "Pruned output dir should exist.");
        // Docker layout has a json/ and a full/ subdirectory.
        Assert.True(Directory.Exists(Path.Combine(outDir, "json")));
        Assert.True(Directory.Exists(Path.Combine(outDir, "full")));
    }

    [Fact]
    public void Raw_Info_Returns_Repo_Context()
    {
        var tool = ResolveTool();
        var plan = Turbo.Raw(tool, "info");
        plan = plan with { WorkingDirectory = _workdir.Value };
        var result = Run(plan);
        Assert.Equal(0, result.ExitCode);
        // turbo info dumps debug context. Loose check — output is non-empty.
        Assert.True(result.StdoutText.Length > 0 || result.StderrText.Length > 0);
    }

    public void Dispose()
    {
        try { Directory.Delete(_workdir.Value, recursive: true); } catch { }
    }
}
