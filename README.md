# Tamp.Turbo

Turborepo CLI wrapper for [Tamp](https://github.com/tamp-build/tamp).

| Package | Turborepo | Status |
|---|---|---|
| [`Tamp.Turbo.V2`](src/Tamp.Turbo.V2) | 2.x | preview |

Requires `Tamp.Core ≥ 1.0.8` (for `[FromNodeModules]` / `[FromPath]` attributes). Remote-cache token typed as `Secret`
(`--token`) — registered with the runner's redaction table.

## Why a separate repo

Turborepo 2.x ships every few weeks. Per the satellite-repo
convention, this tracks Turbo's release cadence independently of
`tamp` core.

## Install

```xml
<PackageVersion Include="Tamp.Turbo.V2" Version="0.2.0" />
```

```xml
<PackageReference Include="Tamp.Turbo.V2" />
```

## Resolving the `turbo` binary

Turborepo is almost always installed as a **workspace devDep** rather
than globally. After `yarn install` (or `npm install`) runs, the
binary lives at `<root>/node_modules/.bin/turbo` (with a `.cmd` shim
on Windows). Use `[FromNodeModules]` to inject it:

```csharp
using Tamp;
using Tamp.NetCli.V10;
using Tamp.Turbo.V2;
using Tamp.Yarn.V4;

[FromPath("yarn")] readonly Tool YarnTool = null!;
[FromNodeModules("turbo")] readonly Tool Turbo = null!;

Target YarnInstall => _ => _.Executes(() =>
    Yarn.Install(YarnTool, s => s.SetImmutable(true)));

// Targets that consume `Turbo` MUST depend on YarnInstall — the binary
// doesn't exist on disk until `yarn install` has run once.
Target FrontendBuild => _ => _
    .DependsOn(nameof(YarnInstall))
    .Executes(() => Tamp.Turbo.V2.Turbo.Run(Turbo, s => s
        .AddTask("build:fast")
        .AddFilter("@holdfast-io/frontend...")
        .SetConcurrency(4)));
```

For globally-installed `turbo` (`brew install turbo`, `npm i -g turbo`,
etc.) use `[FromPath("turbo")]` instead:

```csharp
[FromPath("turbo")] readonly Tool Turbo = null!;
```

Both attributes probe `.cmd` / `.exe` / `.bat` / `.ps1` on Windows and
the extension-less name on POSIX.

## Verbs

| Verb | Notes |
|---|---|
| `Run` | Task execution with `.AddTask(...)`, `.AddFilter(...)`, `.SetConcurrency(...)`, `.SetParallel()`, cache modes, pass-through args after `--`. |
| `Prune` | `--docker` Dockerfile-staging layout — what HoldFast's frontend Dockerfile uses. |
| `Ls` | List packages (experimental in 2.x). |
| `Info` | Debug context dump. |
| `Daemon` | Status / Start / Stop / Restart / Logs / Clean. |
| `Login` / `Logout` / `Link` / `Unlink` | Vercel remote-cache auth lifecycle. |
| `Raw` | Escape hatch for verbs we haven't typed. |

Settings methods are **collection-shaped** for multi-value flags:
`AddTask` / `AddFilter` / `AddScope` (NOT `SetTask` / `SetFilter`).
Call them once per value.

## Quick example — HoldFast frontend build

```csharp
using Tamp;
using Tamp.Turbo.V2;

[FromNodeModules("turbo")] readonly Tool Turbo = null!;

[Secret("Turborepo remote cache", EnvironmentVariable = "TURBO_TOKEN")]
readonly Secret? TurboToken = null;

AbsolutePath Frontend => RootDirectory / "src" / "frontend";

Target FrontendBuild => _ => _
    .DependsOn(nameof(YarnInstall))
    .Executes(() => Tamp.Turbo.V2.Turbo.Run(Turbo, s => s
        .SetWorkingDirectory(Frontend)
        .AddTask("build:fast")
        .AddFilter("@holdfast-io/frontend...")
        .SetConcurrency(4)
        .SetContinue()
        .SetToken(TurboToken!)));

Target DockerStage => _ => _
    .DependsOn(nameof(YarnInstall))
    .Executes(() => Tamp.Turbo.V2.Turbo.Prune(Turbo, s => s
        .SetWorkingDirectory(Frontend)
        .AddScope("@holdfast-io/frontend")
        .SetDocker()
        .SetOutDir("out")));
```

## Cache mode

`SetCache(TurboCacheMode)` maps to Turbo's `--cache=` spec:

| `TurboCacheMode` | Turbo flag |
|---|---|
| `LocalAndRemote` (default) | `--cache=local:rw,remote:rw` |
| `LocalOnly` | `--cache=local:rw,remote:` |
| `RemoteOnly` | `--cache=local:,remote:rw` |
| `None` | `--no-cache` |

## See also

- [tamp](https://github.com/tamp-build/tamp)
- [Turborepo docs](https://turborepo.com/) — task graph, filtering syntax, remote-caching

## Settings authoring style

Examples above use the fluent `Set*`-chain shape. Every wrapper verb also accepts a `new XxxSettings { ... }` object-init form — both produce identical `CommandPlan`s. The fluent shape stays canonical in docs and the `tamp init` template; opt into object-init scaffolding via `tamp init --settings-style=init`.

See [Build Script Authoring → Two authoring styles](https://github.com/tamp-build/tamp/wiki/Build-Script-Authoring#two-authoring-styles-for-wrapper-calls-120) on the wiki for the side-by-side comparison.

## License

[MIT](LICENSE) — same as `tamp` core. (Turborepo itself is MIT.)
