namespace Tamp.Turbo.V2;

/// <summary>Settings for <c>turbo prune [&lt;scope&gt;]</c> — produces a pruned monorepo subset, optionally Docker-shaped.</summary>
/// <remarks>
/// HoldFast uses <c>turbo prune --docker</c> in its Dockerfile stages
/// (HOL-49 was the bug where stale host-side <c>out/</c> leaked into
/// the Docker build context). Critical verb.
/// </remarks>
public sealed class TurboPruneSettings : TurboSettingsBase
{
    /// <summary>Workspaces to include in the prune (positional). Common: a single root package name.</summary>
    public List<string> Scopes { get; } = [];

    /// <summary>Generate a Dockerfile-friendly output layout. Maps to <c>--docker</c>.</summary>
    public bool Docker { get; set; }

    /// <summary>Output directory. Maps to <c>--out-dir</c>. Default is <c>out</c>.</summary>
    public string? OutDir { get; set; }

    /// <summary>Honor <c>.gitignore</c> patterns when computing the prune set. Maps to <c>--use-gitignore</c>.</summary>
    public bool? UseGitignore { get; set; }

    public TurboPruneSettings AddScope(string scope) { Scopes.Add(scope); return this; }
    public TurboPruneSettings SetDocker(bool v = true) { Docker = v; return this; }
    public TurboPruneSettings SetOutDir(string? dir) { OutDir = dir; return this; }
    public TurboPruneSettings SetUseGitignore(bool v) { UseGitignore = v; return this; }

    protected override IEnumerable<string> BuildArguments()
    {
        var args = new List<string> { "prune" };
        foreach (var s in Scopes) args.Add(s);
        if (Docker) args.Add("--docker");
        if (!string.IsNullOrEmpty(OutDir)) { args.Add("--out-dir"); args.Add(OutDir!); }
        if (UseGitignore is { } g) args.Add(g ? "--use-gitignore=true" : "--use-gitignore=false");
        EmitCommonArguments(args);
        return args;
    }
}

/// <summary>Settings for <c>turbo ls</c> (experimental — list packages in the monorepo).</summary>
public sealed class TurboLsSettings : TurboSettingsBase
{
    /// <summary>Filter to a subset of workspaces. Maps to <c>--filter</c>, repeated.</summary>
    public List<string> Filters { get; } = [];

    public TurboLsSettings AddFilter(string filter) { Filters.Add(filter); return this; }

    protected override IEnumerable<string> BuildArguments()
    {
        var args = new List<string> { "ls" };
        foreach (var f in Filters) { args.Add("--filter"); args.Add(f); }
        EmitCommonArguments(args);
        return args;
    }
}

/// <summary>Settings for <c>turbo info</c> — debugging context dump.</summary>
public sealed class TurboInfoSettings : TurboSettingsBase
{
    /// <summary>Specific workspace to print info for. Maps to a positional workspace name.</summary>
    public string? Workspace { get; set; }

    public TurboInfoSettings SetWorkspace(string? name) { Workspace = name; return this; }

    protected override IEnumerable<string> BuildArguments()
    {
        var args = new List<string> { "info" };
        if (!string.IsNullOrEmpty(Workspace)) args.Add(Workspace!);
        EmitCommonArguments(args);
        return args;
    }
}

/// <summary>Daemon subcommands for <c>turbo daemon</c>.</summary>
public enum TurboDaemonSubcommand
{
    /// <summary>No subcommand — print status.</summary>
    Status,
    /// <summary>Start the daemon.</summary>
    Start,
    /// <summary>Stop the daemon.</summary>
    Stop,
    /// <summary>Restart the daemon.</summary>
    Restart,
    /// <summary>Show logs.</summary>
    Logs,
    /// <summary>Clean up daemon state.</summary>
    Clean,
}

/// <summary>Settings for <c>turbo daemon [subcommand]</c>.</summary>
public sealed class TurboDaemonSettings : TurboSettingsBase
{
    public TurboDaemonSubcommand? Subcommand { get; set; }

    public TurboDaemonSettings SetSubcommand(TurboDaemonSubcommand sub) { Subcommand = sub; return this; }

    protected override IEnumerable<string> BuildArguments()
    {
        var args = new List<string> { "daemon" };
        if (Subcommand is { } s)
            args.Add(s switch
            {
                TurboDaemonSubcommand.Status => "status",
                TurboDaemonSubcommand.Start => "start",
                TurboDaemonSubcommand.Stop => "stop",
                TurboDaemonSubcommand.Restart => "restart",
                TurboDaemonSubcommand.Logs => "logs",
                TurboDaemonSubcommand.Clean => "clean",
                _ => throw new ArgumentOutOfRangeException(nameof(s), s, "Unknown daemon subcommand."),
            });
        EmitCommonArguments(args);
        return args;
    }
}

/// <summary>Settings for <c>turbo login</c> / <c>turbo logout</c> / <c>turbo link</c> / <c>turbo unlink</c>. All take just the common flags + optional --sso-team.</summary>
public sealed class TurboLoginSettings : TurboSettingsBase
{
    /// <summary>SSO team slug for organization-scoped logins. Maps to <c>--sso-team</c>.</summary>
    public string? SsoTeam { get; set; }

    public TurboLoginSettings SetSsoTeam(string? team) { SsoTeam = team; return this; }

    protected override IEnumerable<string> BuildArguments()
    {
        var args = new List<string> { "login" };
        if (!string.IsNullOrEmpty(SsoTeam)) { args.Add("--sso-team"); args.Add(SsoTeam!); }
        EmitCommonArguments(args);
        return args;
    }
}

/// <summary>Settings for <c>turbo logout</c>.</summary>
public sealed class TurboLogoutSettings : TurboSettingsBase
{
    protected override IEnumerable<string> BuildArguments()
    {
        var args = new List<string> { "logout" };
        EmitCommonArguments(args);
        return args;
    }
}

/// <summary>Settings for <c>turbo link</c>.</summary>
public sealed class TurboLinkSettings : TurboSettingsBase
{
    /// <summary>Skip the linking prompt. Maps to <c>--yes</c>.</summary>
    public bool Yes { get; set; }

    /// <summary>Scope of resources to link. Maps to <c>--scope</c>.</summary>
    public string? Scope { get; set; }

    public TurboLinkSettings SetYes(bool v = true) { Yes = v; return this; }
    public TurboLinkSettings SetScope(string? scope) { Scope = scope; return this; }

    protected override IEnumerable<string> BuildArguments()
    {
        var args = new List<string> { "link" };
        if (Yes) args.Add("--yes");
        if (!string.IsNullOrEmpty(Scope)) { args.Add("--scope"); args.Add(Scope!); }
        EmitCommonArguments(args);
        return args;
    }
}

/// <summary>Settings for <c>turbo unlink</c>.</summary>
public sealed class TurboUnlinkSettings : TurboSettingsBase
{
    protected override IEnumerable<string> BuildArguments()
    {
        var args = new List<string> { "unlink" };
        EmitCommonArguments(args);
        return args;
    }
}

/// <summary>Escape-hatch settings.</summary>
public sealed class TurboRawSettings : TurboSettingsBase
{
    public List<string> Arguments { get; } = [];

    public TurboRawSettings AddArg(string arg) { Arguments.Add(arg); return this; }
    public TurboRawSettings AddArgs(params string[] args) { Arguments.AddRange(args); return this; }
    public TurboRawSettings AddArgs(IEnumerable<string> args) { Arguments.AddRange(args); return this; }

    protected override IEnumerable<string> BuildArguments()
    {
        if (Arguments.Count == 0) throw new InvalidOperationException("turbo raw: at least one argument is required.");
        return Arguments;
    }
}
