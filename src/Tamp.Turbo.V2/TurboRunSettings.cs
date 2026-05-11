namespace Tamp.Turbo.V2;

/// <summary>Cache behavior for <c>turbo run</c>. Maps to <c>--cache</c>.</summary>
public enum TurboCacheMode
{
    /// <summary>Read + write to both local and remote.</summary>
    LocalAndRemote,
    /// <summary>Local only (<c>--cache=local:rw</c>).</summary>
    LocalOnly,
    /// <summary>Remote only (<c>--cache=remote:rw,local:</c>).</summary>
    RemoteOnly,
    /// <summary>No caching (<c>--no-cache</c>).</summary>
    None,
}

/// <summary>Settings for <c>turbo run [tasks...]</c>.</summary>
public sealed class TurboRunSettings : TurboSettingsBase
{
    /// <summary>Task names to run (e.g. <c>build</c>, <c>build:fast</c>, <c>test</c>). At least one required.</summary>
    public List<string> Tasks { get; } = [];

    /// <summary>Filter expressions. Repeated as <c>--filter &lt;expr&gt;</c>. HoldFast's <c>@holdfast-io/frontend...</c> shape goes here.</summary>
    public List<string> Filters { get; } = [];

    /// <summary>Max concurrent tasks. Maps to <c>--concurrency</c>.</summary>
    public string? Concurrency { get; set; }

    /// <summary>Run tasks in parallel without dependency ordering. Maps to <c>--parallel</c>.</summary>
    public bool Parallel { get; set; }

    /// <summary>Continue running tasks after a failure. Maps to <c>--continue</c>.</summary>
    public bool Continue { get; set; }

    /// <summary>Force re-execution (ignore cache). Maps to <c>--force</c>.</summary>
    public bool Force { get; set; }

    /// <summary>Dry-run mode (compute the plan, run nothing). Maps to <c>--dry-run</c>. Accepts an optional format (e.g. <c>json</c>).</summary>
    public string? DryRun { get; set; }

    /// <summary>Output the task graph. Maps to <c>--graph</c>. Empty string for stdout; path for file output.</summary>
    public string? Graph { get; set; }

    /// <summary>Cache mode.</summary>
    public TurboCacheMode? Cache { get; set; }

    /// <summary>Bypass the local cache, force remote-only reads/writes. Maps to <c>--remote-only</c>.</summary>
    public bool RemoteOnly { get; set; }

    /// <summary>Treat the remote cache as read-only. Maps to <c>--remote-cache-read-only</c>.</summary>
    public bool RemoteCacheReadOnly { get; set; }

    /// <summary>Local cache directory override. Maps to <c>--cache-dir</c>.</summary>
    public string? CacheDir { get; set; }

    /// <summary>Remote-cache request timeout (seconds). Maps to <c>--remote-cache-timeout</c>.</summary>
    public int? RemoteCacheTimeout { get; set; }

    /// <summary>Generate a summary report. Maps to <c>--summarize</c>.</summary>
    public bool Summarize { get; set; }

    /// <summary>Use the Turborepo daemon. Maps to <c>--daemon</c>.</summary>
    public bool Daemon { get; set; }

    /// <summary>Don't use the daemon. Maps to <c>--no-daemon</c>.</summary>
    public bool NoDaemon { get; set; }

    /// <summary>Run before tasks to test cache connectivity. Maps to <c>--preflight</c>.</summary>
    public bool Preflight { get; set; }

    /// <summary>Single-package mode (no monorepo). Maps to <c>--single-package</c>.</summary>
    public bool SinglePackage { get; set; }

    /// <summary>Env-handling mode (<c>loose</c>, <c>strict</c>). Maps to <c>--env-mode</c>.</summary>
    public string? EnvMode { get; set; }

    /// <summary>Globs declaring inputs that affect the whole pipeline. Maps to <c>--global-deps</c>.</summary>
    public List<string> GlobalDeps { get; } = [];

    /// <summary>Arguments after a literal <c>--</c> get passed through to the underlying task scripts.</summary>
    public List<string> PassThroughArgs { get; } = [];

    public TurboRunSettings AddTask(string task) { Tasks.Add(task); return this; }
    public TurboRunSettings AddTasks(IEnumerable<string> tasks) { Tasks.AddRange(tasks); return this; }
    public TurboRunSettings AddFilter(string filter) { Filters.Add(filter); return this; }
    public TurboRunSettings SetConcurrency(string spec) { Concurrency = spec; return this; }
    public TurboRunSettings SetConcurrency(int n) { Concurrency = n.ToString(); return this; }
    public TurboRunSettings SetParallel(bool v = true) { Parallel = v; return this; }
    public TurboRunSettings SetContinue(bool v = true) { Continue = v; return this; }
    public TurboRunSettings SetForce(bool v = true) { Force = v; return this; }
    public TurboRunSettings SetDryRun(string? format = null) { DryRun = format ?? ""; return this; }
    public TurboRunSettings SetGraph(string? destination = null) { Graph = destination ?? ""; return this; }
    public TurboRunSettings SetCache(TurboCacheMode mode) { Cache = mode; return this; }
    public TurboRunSettings SetRemoteOnly(bool v = true) { RemoteOnly = v; return this; }
    public TurboRunSettings SetRemoteCacheReadOnly(bool v = true) { RemoteCacheReadOnly = v; return this; }
    public TurboRunSettings SetCacheDir(string? dir) { CacheDir = dir; return this; }
    public TurboRunSettings SetRemoteCacheTimeout(int seconds) { RemoteCacheTimeout = seconds; return this; }
    public TurboRunSettings SetSummarize(bool v = true) { Summarize = v; return this; }
    public TurboRunSettings SetDaemon(bool v = true) { Daemon = v; return this; }
    public TurboRunSettings SetNoDaemon(bool v = true) { NoDaemon = v; return this; }
    public TurboRunSettings SetPreflight(bool v = true) { Preflight = v; return this; }
    public TurboRunSettings SetSinglePackage(bool v = true) { SinglePackage = v; return this; }
    public TurboRunSettings SetEnvMode(string mode) { EnvMode = mode; return this; }
    public TurboRunSettings AddGlobalDep(string dep) { GlobalDeps.Add(dep); return this; }
    public TurboRunSettings AddPassThroughArg(string arg) { PassThroughArgs.Add(arg); return this; }
    public TurboRunSettings AddPassThroughArgs(IEnumerable<string> args) { PassThroughArgs.AddRange(args); return this; }

    protected override IEnumerable<string> BuildArguments()
    {
        if (Tasks.Count == 0)
            throw new InvalidOperationException("turbo run: at least one task is required.");

        var args = new List<string> { "run" };
        foreach (var t in Tasks) args.Add(t);
        foreach (var f in Filters) { args.Add("--filter"); args.Add(f); }
        if (!string.IsNullOrEmpty(Concurrency)) { args.Add("--concurrency"); args.Add(Concurrency!); }
        if (Parallel) args.Add("--parallel");
        if (Continue) args.Add("--continue");
        if (Force) args.Add("--force");
        if (DryRun is not null)
            args.Add(string.IsNullOrEmpty(DryRun) ? "--dry-run" : $"--dry-run={DryRun}");
        if (Graph is not null)
            args.Add(string.IsNullOrEmpty(Graph) ? "--graph" : $"--graph={Graph}");
        if (Cache is { } c)
        {
            switch (c)
            {
                case TurboCacheMode.LocalAndRemote: args.Add("--cache=local:rw,remote:rw"); break;
                case TurboCacheMode.LocalOnly: args.Add("--cache=local:rw,remote:"); break;
                case TurboCacheMode.RemoteOnly: args.Add("--cache=local:,remote:rw"); break;
                case TurboCacheMode.None: args.Add("--no-cache"); break;
            }
        }
        if (RemoteOnly) args.Add("--remote-only");
        if (RemoteCacheReadOnly) args.Add("--remote-cache-read-only");
        if (!string.IsNullOrEmpty(CacheDir)) { args.Add("--cache-dir"); args.Add(CacheDir!); }
        if (RemoteCacheTimeout is { } rct) { args.Add("--remote-cache-timeout"); args.Add(rct.ToString()); }
        if (Summarize) args.Add("--summarize");
        if (Daemon) args.Add("--daemon");
        if (NoDaemon) args.Add("--no-daemon");
        if (Preflight) args.Add("--preflight");
        if (SinglePackage) args.Add("--single-package");
        if (!string.IsNullOrEmpty(EnvMode)) { args.Add("--env-mode"); args.Add(EnvMode!); }
        foreach (var d in GlobalDeps) { args.Add("--global-deps"); args.Add(d); }
        EmitCommonArguments(args);
        if (PassThroughArgs.Count > 0)
        {
            args.Add("--");
            foreach (var a in PassThroughArgs) args.Add(a);
        }
        return args;
    }
}
