# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/).

## [Unreleased]

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
