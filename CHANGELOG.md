# Changelog

All notable changes to Tamp.Turbo.V2 are documented here.

## [Unreleased]

## [0.2.2] — 2026-09-27

### Added

- Package now ships XML documentation files (`.xml`) alongside the assembly, so consumers get IntelliSense and API docs. (Mirrors [tamp-build/tamp#3](https://github.com/tamp-build/tamp/pull/50).)


## [0.2.1] - 2026-05-11

### Added
- Object-init overloads on every Turbo wrapper (TAM-161 satellite fanout).

### Fixed
- Collapsed duplicate `<Version>` element in `Directory.Build.props` (TAM-81 canonical entry was being shadowed by a trailing `0.0.x-alpha` block under MSBuild last-wins). Single source of truth restored.
