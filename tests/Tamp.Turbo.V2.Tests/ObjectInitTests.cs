using Xunit;

namespace Tamp.Turbo.V2.Tests;

// ---- Object-init overloads (TAM-161) ----

public sealed class ObjectInitTests
{
    private static Tool FakeTool() => new(AbsolutePath.Create("/fake/turbo"));

    [Fact]
    public void Run_ObjectInit_Emits_Identical_Arguments_To_Fluent()
    {
        var tool = FakeTool();

        var fluent = Turbo.Run(tool, s => s
            .AddTask("build:fast")
            .AddFilter("@holdfast-io/frontend...")
            .SetConcurrency(4)
            .SetContinue()
            .SetCwd("/repo")
            .SetEnvMode("strict"));

        var objectInit = Turbo.Run(tool, new TurboRunSettings
        {
            Tasks = { "build:fast" },
            Filters = { "@holdfast-io/frontend..." },
            Concurrency = "4",
            Continue = true,
            Cwd = "/repo",
            EnvMode = "strict",
        });

        Assert.Equal(fluent.Executable, objectInit.Executable);
        Assert.Equal(fluent.Arguments, objectInit.Arguments);
    }

    [Fact]
    public void Every_ObjectInit_Overload_Returns_NonNull_CommandPlan()
    {
        // Smoke test: each wrapper accepts an object-init settings argument and returns a non-null CommandPlan.
        var tool = FakeTool();
        Assert.NotNull(Turbo.Run(tool, new TurboRunSettings { Tasks = { "build" } }));
        Assert.NotNull(Turbo.Prune(tool, new TurboPruneSettings { Docker = true }));
        Assert.NotNull(Turbo.Ls(tool, new TurboLsSettings()));
        Assert.NotNull(Turbo.Info(tool, new TurboInfoSettings { Workspace = "@app/web" }));
        Assert.NotNull(Turbo.Daemon(tool, new TurboDaemonSettings { Subcommand = TurboDaemonSubcommand.Status }));
        Assert.NotNull(Turbo.Login(tool, new TurboLoginSettings { SsoTeam = "acme" }));
        Assert.NotNull(Turbo.Logout(tool, new TurboLogoutSettings()));
        Assert.NotNull(Turbo.Link(tool, new TurboLinkSettings { Yes = true }));
        Assert.NotNull(Turbo.Unlink(tool, new TurboUnlinkSettings()));
    }
}
