namespace Tamp.Turbo.V2;

/// <summary>Facade for Turborepo 2.x CLI verbs.</summary>
/// <remarks>
/// <para>Resolve via <c>[NuGetPackage(UseSystemPath = true)]</c> — typically installed globally via npm or comes from the project's node_modules:</para>
/// <code>
/// [NuGetPackage("turbo", UseSystemPath = true)]
/// readonly Tool Turbo;
/// </code>
/// </remarks>
public static class Turbo
{
    /// <summary><c>turbo run [tasks...]</c></summary>
    public static CommandPlan Run(Tool tool, Action<TurboRunSettings> configure)
        => Build<TurboRunSettings>(tool, configure);

    /// <summary><c>turbo prune [scope]</c> — with --docker, the Dockerfile-staging shape HoldFast uses.</summary>
    public static CommandPlan Prune(Tool tool, Action<TurboPruneSettings>? configure = null)
        => Build<TurboPruneSettings>(tool, configure);

    /// <summary><c>turbo ls</c> — list packages in the monorepo (experimental in 2.x).</summary>
    public static CommandPlan Ls(Tool tool, Action<TurboLsSettings>? configure = null)
        => Build<TurboLsSettings>(tool, configure);

    /// <summary><c>turbo info</c> — debugging context dump.</summary>
    public static CommandPlan Info(Tool tool, Action<TurboInfoSettings>? configure = null)
        => Build<TurboInfoSettings>(tool, configure);

    /// <summary><c>turbo daemon [subcommand]</c> — background daemon lifecycle.</summary>
    public static CommandPlan Daemon(Tool tool, Action<TurboDaemonSettings>? configure = null)
        => Build<TurboDaemonSettings>(tool, configure);

    /// <summary><c>turbo login</c></summary>
    public static CommandPlan Login(Tool tool, Action<TurboLoginSettings>? configure = null)
        => Build<TurboLoginSettings>(tool, configure);

    /// <summary><c>turbo logout</c></summary>
    public static CommandPlan Logout(Tool tool, Action<TurboLogoutSettings>? configure = null)
        => Build<TurboLogoutSettings>(tool, configure);

    /// <summary><c>turbo link</c></summary>
    public static CommandPlan Link(Tool tool, Action<TurboLinkSettings>? configure = null)
        => Build<TurboLinkSettings>(tool, configure);

    /// <summary><c>turbo unlink</c></summary>
    public static CommandPlan Unlink(Tool tool, Action<TurboUnlinkSettings>? configure = null)
        => Build<TurboUnlinkSettings>(tool, configure);

    /// <summary>Escape hatch for verbs we haven't typed.</summary>
    public static CommandPlan Raw(Tool tool, params string[] arguments)
    {
        if (tool is null) throw new ArgumentNullException(nameof(tool));
        if (arguments is null || arguments.Length == 0)
            throw new ArgumentException("Raw requires at least one argument.", nameof(arguments));
        var s = new TurboRawSettings();
        s.AddArgs(arguments);
        return s.ToCommandPlan(tool);
    }

    private static CommandPlan Build<T>(Tool tool, Action<T>? configure) where T : TurboSettingsBase, new()
    {
        if (tool is null) throw new ArgumentNullException(nameof(tool));
        var s = new T();
        configure?.Invoke(s);
        return s.ToCommandPlan(tool);
    }

    // ---- Object-init overloads (TAM-161) ----
    // Two equivalent authoring styles; both produce identical CommandPlans. Fluent
    // stays canonical in docs and `tamp init` templates; object-init available for
    // consumers who prefer the C# initializer shape.
    //
    //     Turbo.Run(tool, new() { Tasks = { "build" }, Filters = { "@app/web" } });
    //
    // is equivalent to:
    //
    //     Turbo.Run(tool, s => s.AddTask("build").AddFilter("@app/web"));

    public static CommandPlan Run(Tool tool, TurboRunSettings settings)
    {
        if (tool is null) throw new ArgumentNullException(nameof(tool));
        if (settings is null) throw new ArgumentNullException(nameof(settings));
        return settings.ToCommandPlan(tool);
    }

    public static CommandPlan Prune(Tool tool, TurboPruneSettings settings)
    {
        if (tool is null) throw new ArgumentNullException(nameof(tool));
        if (settings is null) throw new ArgumentNullException(nameof(settings));
        return settings.ToCommandPlan(tool);
    }

    public static CommandPlan Ls(Tool tool, TurboLsSettings settings)
    {
        if (tool is null) throw new ArgumentNullException(nameof(tool));
        if (settings is null) throw new ArgumentNullException(nameof(settings));
        return settings.ToCommandPlan(tool);
    }

    public static CommandPlan Info(Tool tool, TurboInfoSettings settings)
    {
        if (tool is null) throw new ArgumentNullException(nameof(tool));
        if (settings is null) throw new ArgumentNullException(nameof(settings));
        return settings.ToCommandPlan(tool);
    }

    public static CommandPlan Daemon(Tool tool, TurboDaemonSettings settings)
    {
        if (tool is null) throw new ArgumentNullException(nameof(tool));
        if (settings is null) throw new ArgumentNullException(nameof(settings));
        return settings.ToCommandPlan(tool);
    }

    public static CommandPlan Login(Tool tool, TurboLoginSettings settings)
    {
        if (tool is null) throw new ArgumentNullException(nameof(tool));
        if (settings is null) throw new ArgumentNullException(nameof(settings));
        return settings.ToCommandPlan(tool);
    }

    public static CommandPlan Logout(Tool tool, TurboLogoutSettings settings)
    {
        if (tool is null) throw new ArgumentNullException(nameof(tool));
        if (settings is null) throw new ArgumentNullException(nameof(settings));
        return settings.ToCommandPlan(tool);
    }

    public static CommandPlan Link(Tool tool, TurboLinkSettings settings)
    {
        if (tool is null) throw new ArgumentNullException(nameof(tool));
        if (settings is null) throw new ArgumentNullException(nameof(settings));
        return settings.ToCommandPlan(tool);
    }

    public static CommandPlan Unlink(Tool tool, TurboUnlinkSettings settings)
    {
        if (tool is null) throw new ArgumentNullException(nameof(tool));
        if (settings is null) throw new ArgumentNullException(nameof(settings));
        return settings.ToCommandPlan(tool);
    }
}
