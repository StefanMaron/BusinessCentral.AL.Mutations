# BusinessCentral.AL.Mutations — Development Guide

## What This Is

A C# .NET global tool (`al-mutate`, NuGet: `MSDyn365BC.AL.Mutate`) for mutation testing
Microsoft Dynamics 365 Business Central AL code.

The core concept: apply mutations, run tests, report survivors.

## Stack

- **C# .NET 8** — `dotnet tool install --global MSDyn365BC.AL.Mutate`
- **`Microsoft.Dynamics.Nav.CodeAnalysis`** — AST-based mutation scanning (`NavSyntaxTree` / `NavSyntaxWalker`)
- **AL Runner** (`../BusinessCentral.AL.Runner`) — project reference for in-process test execution
- **`git checkout -- <file>`** — mutation restore (requires clean working tree — enforced at startup)

> The old Python implementation in `al_mutate/` is gone. The old PowerShell module (`BCMutations/`) is also gone.

## Solution Structure

```
AlMutate/
  AlMutate.slnx                 # Solution file
  AlMutate/                     # Main tool project
    operators/
      default.json              # Embedded resource — default mutation operators
  AlMutate.Tests/               # xUnit test project
  tests/                        # Integration test fixtures (AL files, etc.)
operators/                      # Repo-level operator reference (mirrors embedded resource)
docs/
  ARCHITECTURE.md
  OPERATORS.md
  USAGE.md
  CSHARP_MIGRATION_PLAN.md      # Archived: migration complete April 2026
```

## Development Principles

**Strict TDD:** Write a failing test first, then implement. Every function must have a test.

**No BC dependency in unit tests:** Unit tests use `FakeTestRunner`. Only integration tests use
the real `AlRunnerPipeline`.

**Fail fast:** Any restore failure or unexpected error aborts immediately. Never leave mutated code in place.

## Running Tests

```bash
# Unit tests only (no BC instance needed)
dotnet test AlMutate/AlMutate.slnx --filter "Category!=Integration"

# All tests (requires a running BC instance)
dotnet test AlMutate/AlMutate.slnx
```

## Key Classes

| Class | Responsibility |
|-------|---------------|
| `MutationPipeline` | Orchestrates the full run: startup → baseline → replay → new mutations → report |
| `AlScanner` | Walks NavSyntaxTree, matches operators to node types, returns candidates |
| `Mutator` | Applies and restores mutations (restore always via `git checkout`) |
| `GitService` | Checks working tree cleanliness, performs restore |
| `AlRunnerTestRunner` | Integrates with AL Runner (`AlRunnerPipeline.Run()`) |
| `MutationLog` | Reads/writes `mutations.json` (append-only) |
| `ReportGenerator` | Generates `report.md` and prints summary |
| `OperatorLoader` | Loads and validates operator JSON (embedded resource or custom file) |

## Operator Format

Operators are in `AlMutate/AlMutate/operators/default.json` (embedded resource):

```json
{
  "operators": [
    {
      "id": "rel-gt-to-gte",
      "name": "Greater-than to greater-or-equal",
      "category": "relational",
      "node_type": "comparison_expression",
      "operator_token": ">",
      "replacement": ">="
    },
    {
      "id": "stmt-remove-error",
      "name": "Remove Error statement",
      "category": "statement-removal",
      "node_type": "call_expression",
      "identifier": "Error",
      "replacement": null
    }
  ]
}
```

`replacement: null` → comment out the entire statement line.

33 operators across 8 categories: relational, arithmetic, logical, boolean,
statement-removal, boundary, control-flow, bc-specific.

## mutations.json Schema

Schema version 1 (unchanged from Python era). Statuses: `KILLED` | `SURVIVED` | `COMPILE_ERROR` | `OBSOLETE`.

`OBSOLETE` = original line no longer exists in the file (production code changed).

## --stubs Flag

Use `--stubs <path>` for repos that contain stub AL files (e.g. Sentinel). This excludes stub
files from mutation scanning so only real implementation code is targeted.

## Execution Flow

```
STARTUP
  - Verify clean git working tree (abort if dirty)
  - Load operator definitions

BASELINE
  - Compile + run tests on unmodified code via AL Runner
  - Abort if baseline fails

REPLAY (if mutations.json exists)
  - For each SURVIVED mutation from last run:
    - Find original string in file (if missing → OBSOLETE)
    - Apply mutation → compile → test → restore
    - Record: KILLED or SURVIVED

NEW MUTATIONS
  - Walk NavSyntaxTree, match operators to node types
  - For each candidate: apply → compile → test → restore
  - Record result

REPORT
  - Append new run to mutations.json
  - Generate report.md
  - Print summary: score, survivors, required fixes
```

## GitHub Actions

| Workflow | Trigger | Purpose |
|----------|---------|---------|
| `test.yml` | push to master | Build + unit tests |
| `mutation-sentinel.yml` | workflow_dispatch | Run mutations against Sentinel |
| `publish.yml` | tag push | Publish NuGet package |
