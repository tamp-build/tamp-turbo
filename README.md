# Tamp.Turbo

Turborepo CLI wrapper for [Tamp](https://github.com/tamp-build/tamp).

| Package | Turborepo | Status |
|---|---|---|
| [`Tamp.Turbo.V2`](src/Tamp.Turbo.V2) | 2.x | preview |

Requires `Tamp.Core ≥ 1.0.3`. Remote-cache token typed as `Secret`
(`--token`) — registered with the runner's redaction table.

## Why a separate repo

Turborepo 2.x ships every few weeks. Per the satellite-repo
convention, this tracks Turbo's release cadence independently of
`tamp` core.

## Install

```xml
<PackageVersion Include="Tamp.Turbo.V2" Version="0.1.0" />
```

```xml
<PackageReference Include="Tamp.Turbo.V2" />
```

## Verbs

| Verb | Notes |
|---|---|
| `Run` | Task execution with `--filter`, `--concurrency`, `--parallel`, cache modes, pass-through args after `--`. |
| `Prune` | `--docker` Dockerfile-staging layout — what HoldFast's frontend Dockerfile uses. |
| `Ls` | List packages (experimental in 2.x). |
| `Info` | Debug context dump. |
| `Daemon` | Status / Start / Stop / Restart / Logs / Clean. |
| `Login` / `Logout` / `Link` / `Unlink` | Vercel remote-cache auth lifecycle. |
| `Raw` | Escape hatch for verbs we haven't typed. |

## Quick example — HoldFast frontend build

```csharp
using Tamp;
using Tamp.Turbo.V2;

[NuGetPackage("turbo", UseSystemPath = true)]
readonly Tool Turbo = null!;

[Secret("Turborepo remote cache", EnvironmentVariable = "TURBO_TOKEN")]
readonly Secret? TurboToken = null!;

AbsolutePath Frontend => RootDirectory / "src" / "frontend";

Target FrontendBuild => _ => _.Executes(() => Tamp.Turbo.V2.Turbo.Run(Turbo, s => s
    .SetWorkingDirectory(Frontend)
    .AddTask("build:fast")
    .AddFilter("@holdfast-io/frontend...")
    .SetConcurrency(4)
    .SetContinue()
    .SetToken(TurboToken!)));

Target DockerStage => _ => _.Executes(() => Tamp.Turbo.V2.Turbo.Prune(Turbo, s => s
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

## License

[MIT](LICENSE) — same as `tamp` core. (Turborepo itself is MIT.)
