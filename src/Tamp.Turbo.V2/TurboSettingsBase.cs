namespace Tamp.Turbo.V2;

/// <summary>
/// Common knobs across every <c>turbo</c> verb. Mostly governs
/// caching, output, and remote-cache auth.
/// </summary>
public abstract class TurboSettingsBase
{
    /// <summary>Working directory of the spawned <c>turbo</c> process.</summary>
    public string? WorkingDirectory { get; set; }

    /// <summary>Per-invocation environment variables.</summary>
    public Dictionary<string, string> EnvironmentVariables { get; } = new();

    /// <summary>Turborepo's <c>--cwd</c> flag — distinct from the spawned-process CWD; this sets the monorepo root the command operates on.</summary>
    public string? Cwd { get; set; }

    /// <summary>Disable color output. Maps to <c>--no-color</c>.</summary>
    public bool NoColor { get; set; }

    /// <summary>Force color even when not a TTY. Maps to <c>--color</c>.</summary>
    public bool Color { get; set; }

    /// <summary>UI mode (<c>tui</c>, <c>stream</c>). Maps to <c>--ui</c>.</summary>
    public string? Ui { get; set; }

    /// <summary>Verbosity level (count). Maps to <c>--verbosity &lt;n&gt;</c>.</summary>
    public int? Verbosity { get; set; }

    /// <summary>Remote-cache API endpoint override. Maps to <c>--api</c>.</summary>
    public string? Api { get; set; }

    /// <summary>Remote-cache team slug. Maps to <c>--team</c>.</summary>
    public string? Team { get; set; }

    /// <summary>Remote-cache login URL. Maps to <c>--login</c>.</summary>
    public string? Login { get; set; }

    /// <summary>Remote-cache auth token. Maps to <c>--token</c>. Pass as <see cref="Secret"/> so it's redacted.</summary>
    public Secret? Token { get; set; }

    /// <summary>Skip Turbo's version-inference probe. Maps to <c>--skip-infer</c>.</summary>
    public bool SkipInfer { get; set; }

    /// <summary>Disable the update notifier. Maps to <c>--no-update-notifier</c>.</summary>
    public bool NoUpdateNotifier { get; set; }

    protected abstract IEnumerable<string> BuildArguments();

    protected virtual IEnumerable<Secret> CollectSecrets()
    {
        if (Token is not null) yield return Token;
    }

    protected void EmitCommonArguments(List<string> args)
    {
        if (!string.IsNullOrEmpty(Cwd)) { args.Add("--cwd"); args.Add(Cwd!); }
        if (NoColor) args.Add("--no-color");
        if (Color) args.Add("--color");
        if (!string.IsNullOrEmpty(Ui)) { args.Add("--ui"); args.Add(Ui!); }
        if (Verbosity is { } v) { args.Add("--verbosity"); args.Add(v.ToString()); }
        if (!string.IsNullOrEmpty(Api)) { args.Add("--api"); args.Add(Api!); }
        if (!string.IsNullOrEmpty(Team)) { args.Add("--team"); args.Add(Team!); }
        if (!string.IsNullOrEmpty(Login)) { args.Add("--login"); args.Add(Login!); }
        if (Token is { } t) { args.Add("--token"); args.Add(t.Reveal()); }
        if (SkipInfer) args.Add("--skip-infer");
        if (NoUpdateNotifier) args.Add("--no-update-notifier");
    }

    internal CommandPlan ToCommandPlan(Tool tool)
    {
        var args = BuildArguments().ToList();
        return new CommandPlan
        {
            Executable = tool.Executable.Value,
            Arguments = args,
            Environment = new Dictionary<string, string>(EnvironmentVariables),
            WorkingDirectory = WorkingDirectory ?? tool.WorkingDirectory,
            Secrets = CollectSecrets().ToList(),
        };
    }
}

public static class TurboSettingsBaseExtensions
{
    public static T SetWorkingDirectory<T>(this T s, string? cwd) where T : TurboSettingsBase { s.WorkingDirectory = cwd; return s; }
    public static T SetEnvironmentVariable<T>(this T s, string name, string value) where T : TurboSettingsBase { s.EnvironmentVariables[name] = value; return s; }
    public static T SetCwd<T>(this T s, string? cwd) where T : TurboSettingsBase { s.Cwd = cwd; return s; }
    public static T SetNoColor<T>(this T s, bool v = true) where T : TurboSettingsBase { s.NoColor = v; return s; }
    public static T SetColor<T>(this T s, bool v = true) where T : TurboSettingsBase { s.Color = v; return s; }
    public static T SetUi<T>(this T s, string? ui) where T : TurboSettingsBase { s.Ui = ui; return s; }
    public static T SetVerbosity<T>(this T s, int count) where T : TurboSettingsBase { s.Verbosity = count; return s; }
    public static T SetApi<T>(this T s, string? api) where T : TurboSettingsBase { s.Api = api; return s; }
    public static T SetTeam<T>(this T s, string? team) where T : TurboSettingsBase { s.Team = team; return s; }
    public static T SetLogin<T>(this T s, string? login) where T : TurboSettingsBase { s.Login = login; return s; }
    public static T SetToken<T>(this T s, Secret token) where T : TurboSettingsBase { s.Token = token; return s; }
    public static T SetSkipInfer<T>(this T s, bool v = true) where T : TurboSettingsBase { s.SkipInfer = v; return s; }
    public static T SetNoUpdateNotifier<T>(this T s, bool v = true) where T : TurboSettingsBase { s.NoUpdateNotifier = v; return s; }
}
