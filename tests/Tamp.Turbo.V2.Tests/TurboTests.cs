using Xunit;

namespace Tamp.Turbo.V2.Tests;

public sealed class TurboTests
{
    private static Tool FakeTool() => new(AbsolutePath.Create("/fake/turbo"));

    private static int IndexOf(IReadOnlyList<string> args, string value, int start = 0)
    {
        for (var i = start; i < args.Count; i++) if (args[i] == value) return i;
        return -1;
    }

    // null-tool guards
    [Fact] public void Run_NullTool() => Assert.Throws<ArgumentNullException>(() => Turbo.Run(null!, s => s.AddTask("x")));
    [Fact] public void Prune_NullTool() => Assert.Throws<ArgumentNullException>(() => Turbo.Prune(null!));
    [Fact] public void Ls_NullTool() => Assert.Throws<ArgumentNullException>(() => Turbo.Ls(null!));
    [Fact] public void Info_NullTool() => Assert.Throws<ArgumentNullException>(() => Turbo.Info(null!));
    [Fact] public void Daemon_NullTool() => Assert.Throws<ArgumentNullException>(() => Turbo.Daemon(null!));
    [Fact] public void Login_NullTool() => Assert.Throws<ArgumentNullException>(() => Turbo.Login(null!));
    [Fact] public void Logout_NullTool() => Assert.Throws<ArgumentNullException>(() => Turbo.Logout(null!));
    [Fact] public void Link_NullTool() => Assert.Throws<ArgumentNullException>(() => Turbo.Link(null!));
    [Fact] public void Unlink_NullTool() => Assert.Throws<ArgumentNullException>(() => Turbo.Unlink(null!));
    [Fact] public void Raw_NullTool() => Assert.Throws<ArgumentNullException>(() => Turbo.Raw(null!, "info"));
    [Fact] public void Raw_NoArgs() => Assert.Throws<ArgumentException>(() => Turbo.Raw(FakeTool()));

    [Theory]
    [InlineData("/usr/local/bin/turbo")]
    [InlineData("/Users/scott/.nvm/versions/node/v24/bin/turbo")]
    public void Every_Verb_Uses_Tool_Path(string toolPath)
    {
        // Path normalization safety net (TAM-84 pattern).
        var tool = new Tool(AbsolutePath.Create(toolPath));
        var expected = tool.Executable.Value;
        Assert.Equal(expected, Turbo.Ls(tool).Executable);
        Assert.Equal(expected, Turbo.Daemon(tool).Executable);
        Assert.Equal(expected, Turbo.Run(tool, s => s.AddTask("build")).Executable);
    }

    // -------- Token (Secret) --------

    [Fact]
    public void Token_Becomes_DashDashToken_And_Registers_As_Secret()
    {
        var token = new Secret("Turborepo remote cache", "trbo_xxx");
        var plan = Turbo.Run(FakeTool(), s => s.AddTask("build").SetToken(token));
        var args = plan.Arguments;
        Assert.Equal("trbo_xxx", args[IndexOf(args, "--token") + 1]);
        Assert.Same(token, Assert.Single(plan.Secrets));
    }

    [Fact]
    public void No_Token_Means_Empty_Secrets()
    {
        var plan = Turbo.Run(FakeTool(), s => s.AddTask("build"));
        Assert.Empty(plan.Secrets);
    }

    // -------- run --------

    [Fact]
    public void Run_Throws_When_No_Tasks()
        => Assert.Throws<InvalidOperationException>(() => Turbo.Run(FakeTool(), _ => { }));

    [Fact]
    public void Run_Verb_Then_Task_Positionals_In_Order()
    {
        var args = Turbo.Run(FakeTool(), s => s.AddTask("build").AddTask("test")).Arguments;
        Assert.Equal("run", args[0]);
        Assert.Equal("build", args[1]);
        Assert.Equal("test", args[2]);
    }

    [Fact]
    public void Run_Filter_Repeated_For_Each_Pattern()
    {
        var args = Turbo.Run(FakeTool(), s => s
            .AddTask("build:fast")
            .AddFilter("@holdfast-io/frontend...")
            .AddFilter("@holdfast-io/browser")).Arguments;
        Assert.Equal(2, args.Count(a => a == "--filter"));
        Assert.Contains("@holdfast-io/frontend...", args);
        Assert.Contains("@holdfast-io/browser", args);
    }

    [Fact]
    public void Run_Concurrency_Accepts_Int_Or_Spec_String()
    {
        var asInt = Turbo.Run(FakeTool(), s => s.AddTask("t").SetConcurrency(8)).Arguments;
        Assert.Equal("8", asInt[IndexOf(asInt, "--concurrency") + 1]);
        // Turbo also accepts spec strings like "50%" — verify pass-through.
        var asStr = Turbo.Run(FakeTool(), s => s.AddTask("t").SetConcurrency("50%")).Arguments;
        Assert.Equal("50%", asStr[IndexOf(asStr, "--concurrency") + 1]);
    }

    [Fact]
    public void Run_DryRun_Bare_Vs_Format()
    {
        var bare = Turbo.Run(FakeTool(), s => s.AddTask("t").SetDryRun()).Arguments;
        Assert.Contains("--dry-run", bare);
        var withFormat = Turbo.Run(FakeTool(), s => s.AddTask("t").SetDryRun("json")).Arguments;
        Assert.Contains("--dry-run=json", withFormat);
    }

    [Fact]
    public void Run_Graph_Bare_Vs_Path()
    {
        var bare = Turbo.Run(FakeTool(), s => s.AddTask("t").SetGraph()).Arguments;
        Assert.Contains("--graph", bare);
        var path = Turbo.Run(FakeTool(), s => s.AddTask("t").SetGraph("graph.html")).Arguments;
        Assert.Contains("--graph=graph.html", path);
    }

    [Theory]
    [InlineData(TurboCacheMode.LocalAndRemote, "--cache=local:rw,remote:rw")]
    [InlineData(TurboCacheMode.LocalOnly, "--cache=local:rw,remote:")]
    [InlineData(TurboCacheMode.RemoteOnly, "--cache=local:,remote:rw")]
    [InlineData(TurboCacheMode.None, "--no-cache")]
    public void Run_Cache_Mode_Maps_To_Turbo_Spec(TurboCacheMode mode, string expected)
    {
        var args = Turbo.Run(FakeTool(), s => s.AddTask("t").SetCache(mode)).Arguments;
        Assert.Contains(expected, args);
    }

    [Fact]
    public void Run_Flags_All_Round_Trip()
    {
        var args = Turbo.Run(FakeTool(), s => s
            .AddTask("build")
            .SetParallel().SetContinue().SetForce()
            .SetRemoteOnly().SetRemoteCacheReadOnly()
            .SetCacheDir("/tmp/turbocache")
            .SetRemoteCacheTimeout(60)
            .SetSummarize()
            .SetDaemon()
            .SetPreflight()
            .SetSinglePackage()
            .SetEnvMode("strict")
            .AddGlobalDep("**/Dockerfile")).Arguments;
        Assert.Contains("--parallel", args);
        Assert.Contains("--continue", args);
        Assert.Contains("--force", args);
        Assert.Contains("--remote-only", args);
        Assert.Contains("--remote-cache-read-only", args);
        Assert.Equal("/tmp/turbocache", args[IndexOf(args, "--cache-dir") + 1]);
        Assert.Equal("60", args[IndexOf(args, "--remote-cache-timeout") + 1]);
        Assert.Contains("--summarize", args);
        Assert.Contains("--daemon", args);
        Assert.Contains("--preflight", args);
        Assert.Contains("--single-package", args);
        Assert.Equal("strict", args[IndexOf(args, "--env-mode") + 1]);
        Assert.Equal("**/Dockerfile", args[IndexOf(args, "--global-deps") + 1]);
    }

    [Fact]
    public void Run_PassThroughArgs_After_DashDashSeparator()
    {
        var args = Turbo.Run(FakeTool(), s => s
            .AddTask("test")
            .AddPassThroughArg("--watch")
            .AddPassThroughArg("--reporter=verbose")).Arguments;
        var sepIdx = IndexOf(args, "--");
        Assert.True(sepIdx > 0);
        Assert.Equal("--watch", args[sepIdx + 1]);
        Assert.Equal("--reporter=verbose", args[sepIdx + 2]);
    }

    [Fact]
    public void Run_HoldFast_Shape_Round_Trips_All_Args()
    {
        // turbo run build:fast --filter=@holdfast-io/frontend... --concurrency 4 --continue
        var args = Turbo.Run(FakeTool(), s => s
            .AddTask("build:fast")
            .AddFilter("@holdfast-io/frontend...")
            .SetConcurrency(4)
            .SetContinue()).Arguments;
        Assert.Equal("run", args[0]);
        Assert.Equal("build:fast", args[1]);
        Assert.Equal("--filter", args[2]);
        Assert.Equal("@holdfast-io/frontend...", args[3]);
        Assert.Equal("4", args[IndexOf(args, "--concurrency") + 1]);
        Assert.Contains("--continue", args);
    }

    // -------- prune --------

    [Fact]
    public void Prune_HoldFast_Docker_Shape()
    {
        // The actual HoldFast Dockerfile invocation:
        // turbo prune @holdfast-io/frontend --docker --out-dir out
        var args = Turbo.Prune(FakeTool(), s => s
            .AddScope("@holdfast-io/frontend")
            .SetDocker()
            .SetOutDir("out")).Arguments;
        Assert.Equal("prune", args[0]);
        Assert.Equal("@holdfast-io/frontend", args[1]);
        Assert.Contains("--docker", args);
        Assert.Equal("out", args[IndexOf(args, "--out-dir") + 1]);
    }

    [Fact]
    public void Prune_UseGitignore_True_And_False_Round_Trip()
    {
        var on = Turbo.Prune(FakeTool(), s => s.SetUseGitignore(true)).Arguments;
        Assert.Contains("--use-gitignore=true", on);
        var off = Turbo.Prune(FakeTool(), s => s.SetUseGitignore(false)).Arguments;
        Assert.Contains("--use-gitignore=false", off);
    }

    // -------- ls --------

    [Fact]
    public void Ls_Bare_Verb_Only()
    {
        var args = Turbo.Ls(FakeTool()).Arguments;
        Assert.Equal(["ls"], args);
    }

    [Fact]
    public void Ls_Filter_Repeated()
    {
        var args = Turbo.Ls(FakeTool(), s => s.AddFilter("@holdfast-io/*").AddFilter("rrweb*")).Arguments;
        Assert.Equal(2, args.Count(a => a == "--filter"));
    }

    // -------- info --------

    [Fact]
    public void Info_Optional_Workspace_Positional()
    {
        var bare = Turbo.Info(FakeTool()).Arguments;
        Assert.Equal(["info"], bare);
        var withWs = Turbo.Info(FakeTool(), s => s.SetWorkspace("@holdfast-io/backend")).Arguments;
        Assert.Equal("info", withWs[0]);
        Assert.Equal("@holdfast-io/backend", withWs[1]);
    }

    // -------- daemon --------

    [Fact]
    public void Daemon_Bare_Has_Only_Verb()
    {
        var args = Turbo.Daemon(FakeTool()).Arguments;
        Assert.Equal(["daemon"], args);
    }

    [Theory]
    [InlineData(TurboDaemonSubcommand.Start, "start")]
    [InlineData(TurboDaemonSubcommand.Stop, "stop")]
    [InlineData(TurboDaemonSubcommand.Restart, "restart")]
    [InlineData(TurboDaemonSubcommand.Logs, "logs")]
    [InlineData(TurboDaemonSubcommand.Clean, "clean")]
    [InlineData(TurboDaemonSubcommand.Status, "status")]
    public void Daemon_Subcommand_Maps_To_Lowercase_Token(TurboDaemonSubcommand sub, string expected)
    {
        var args = Turbo.Daemon(FakeTool(), s => s.SetSubcommand(sub)).Arguments;
        Assert.Equal(expected, args[1]);
    }

    // -------- login / logout / link / unlink --------

    [Fact]
    public void Login_SsoTeam_Round_Trip()
    {
        var args = Turbo.Login(FakeTool(), s => s.SetSsoTeam("acme")).Arguments;
        Assert.Equal("login", args[0]);
        Assert.Equal("acme", args[IndexOf(args, "--sso-team") + 1]);
    }

    [Fact]
    public void Logout_Verb_Only() => Assert.Equal(["logout"], Turbo.Logout(FakeTool()).Arguments);

    [Fact]
    public void Link_Yes_And_Scope_Round_Trip()
    {
        var args = Turbo.Link(FakeTool(), s => s.SetYes().SetScope("acme")).Arguments;
        Assert.Equal("link", args[0]);
        Assert.Contains("--yes", args);
        Assert.Equal("acme", args[IndexOf(args, "--scope") + 1]);
    }

    [Fact]
    public void Unlink_Verb_Only() => Assert.Equal(["unlink"], Turbo.Unlink(FakeTool()).Arguments);

    // -------- raw --------

    [Fact]
    public void Raw_Passes_Args_Verbatim()
    {
        var args = Turbo.Raw(FakeTool(), "completion", "bash").Arguments;
        Assert.Equal(["completion", "bash"], args);
    }

    // -------- common settings --------

    [Fact]
    public void Common_Flags_Round_Trip()
    {
        var args = Turbo.Run(FakeTool(), s => s
            .AddTask("build")
            .SetCwd("/repo")
            .SetNoColor()
            .SetUi("stream")
            .SetVerbosity(2)
            .SetApi("https://cache.example.com")
            .SetTeam("acme")
            .SetSkipInfer()
            .SetNoUpdateNotifier()).Arguments;
        Assert.Equal("/repo", args[IndexOf(args, "--cwd") + 1]);
        Assert.Contains("--no-color", args);
        Assert.Equal("stream", args[IndexOf(args, "--ui") + 1]);
        Assert.Equal("2", args[IndexOf(args, "--verbosity") + 1]);
        Assert.Equal("https://cache.example.com", args[IndexOf(args, "--api") + 1]);
        Assert.Equal("acme", args[IndexOf(args, "--team") + 1]);
        Assert.Contains("--skip-infer", args);
        Assert.Contains("--no-update-notifier", args);
    }

    [Fact]
    public void WorkingDirectory_Settings_Wins_Over_Tool()
    {
        var tool = new Tool(AbsolutePath.Create("/fake/turbo"), workingDirectory: "/from-tool");
        var plan = Turbo.Ls(tool, s => s.SetWorkingDirectory("/from-settings"));
        Assert.Equal("/from-settings", plan.WorkingDirectory);
    }
}
