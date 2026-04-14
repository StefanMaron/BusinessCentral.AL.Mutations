# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/).

## [Unreleased]

## [0.2.0] - 2026-04-14

### Added
- `replay` command to re-test survived mutations without running new ones
- AL Runner server mode for incremental compilation — mutations compile significantly faster
- Per-mutation timeout with `TIMED_OUT` status
- `--silent` flag to suppress per-mutation progress output
- `--guide` flag for contextual help
- Elapsed time displayed per mutation in progress output
- Self-contained dependency loading; al-runner invoked as subprocess

### Changed
- Operator set expanded from 33 to 55 operators
- String-concat skip is now opt-in via `skip_string_operands` operator flag (was always-on)

### Fixed
- `report.md` is now written next to `mutations.json` instead of the working directory
- AL Runner server is restarted automatically on timeout or exception
- Mutation IDs increment correctly during a run; server response timeout added
- Compile-error mutations eliminated from string concatenation, attributes, and duplicates
- `stmt-remove-get` operator removed — `Get()` is almost never a standalone statement in AL
- Correct `DownloadArtifacts` path in `EnsureALCompiler` MSBuild target
- Pin al-runner to 1.0.5; use full path after install; log stderr on failure

## [0.1.0] - 2026-04-12

### Added
- Complete rewrite as C# .NET tool (`al-mutate`, NuGet: `MSDyn365BC.AL.Mutate`)
- AST-based mutation scanning via `Microsoft.Dynamics.Nav.CodeAnalysis` (NavSyntaxTree / NavSyntaxWalker)
- AL Runner integration for in-process test execution (no BC container required)
- 33 mutation operators across 8 categories (relational, arithmetic, logical, boolean, statement-removal, boundary, control-flow, bc-specific)
- `--stubs` flag for repos with stub AL files
- Append-only `mutations.json` log with replay support
- Markdown report generation with mutation score
- 77 tests (xUnit, strict red/green TDD)
- GitHub Actions: push tests (`test.yml`), on-demand Sentinel mutation run (`mutation-sentinel.yml`), NuGet publish on tag (`publish.yml`)

### Removed
- Python implementation (`al_mutate/` package)
- PowerShell module (`BCMutations/`) — removed in earlier release
- Dependency on BC Linux stack (`al-compile`, `bc-publish`, `run-tests.sh`)
