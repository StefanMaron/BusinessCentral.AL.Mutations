# Architecture

## Overview

al-mutate is a C# .NET global tool that performs mutation testing on Business Central AL code.
It parses AL source files using `Microsoft.Dynamics.Nav.CodeAnalysis`, identifies mutation
targets in the syntax tree, applies mutations one at a time, and runs the test suite via
AL Runner to check if each mutation is caught.

## Design Principles

1. **AST-based targeting** — Mutations are identified by walking the `NavSyntaxTree` and matching
   operator node types. This eliminates false positives from object properties, attributes,
   comments, and string literals.
2. **Git-based restore** — Mutated files are always restored via `git checkout -- <file>`, never
   by rewriting. This guarantees clean state.
3. **Fail fast** — Any restore failure or unexpected error aborts immediately. Never leave mutated
   code in place.
4. **Append-only log** — Results accumulate across runs in `mutations.json`. Previously-survived
   mutations can be replayed.

## Solution Structure

```
AlMutate/
  AlMutate.slnx                 # Solution file
  AlMutate/                     # Main tool project
    operators/
      default.json              # Embedded resource — default mutation operators
  AlMutate.Tests/               # xUnit test project
  tests/                        # Integration test fixtures (AL files, etc.)
```

## Key Classes

| Class | Responsibility |
|-------|---------------|
| `MutationPipeline` | Orchestrates the full run: startup → baseline → replay → new mutations → report |
| `AlScanner` | Walks NavSyntaxTree via NavSyntaxWalker, matches operators to node types, returns candidates |
| `Mutator` | Applies mutations to source files and records originals |
| `GitService` | Checks working tree cleanliness; restores files via `git checkout -- <file>` |
| `AlRunnerTestRunner` | Wraps AL Runner integration (`AlRunnerPipeline.Run()`) for test execution |
| `MutationLog` | Reads/writes `mutations.json` (append-only, schema_version 1) |
| `ReportGenerator` | Generates `report.md` and prints score summary to stdout |
| `OperatorLoader` | Loads and validates operator JSON from embedded resource or custom file |

## AST-Based Scanning

The scanner uses `Microsoft.Dynamics.Nav.CodeAnalysis` to parse each `.al` file into a full
NavSyntaxTree. A custom `NavSyntaxWalker` subclass visits nodes and matches them against the
loaded operator definitions:

- `comparison_expression` — relational operators (`>`, `<`, `>=`, `<=`, `=`, `<>`)
- `additive_expression` — `+`, `-`
- `multiplicative_expression` — `*`, `/`, `mod`, `div`
- `logical_expression` — `and`, `or`
- `unary_expression` — `not`
- `call_expression` — method calls (statement removal, BC-specific)

Because the AL compiler's syntax tree distinguishes executable code from metadata, comments,
and strings, no separate context filtering is needed.

## AL Runner Integration

Test execution is handled by a project reference to `../BusinessCentral.AL.Runner`.
The `AlRunnerTestRunner` class calls `AlRunnerPipeline.Run()` with the test app path and
connection settings. This runs tests in-process — no external shell scripts or BC container
tooling required.

## Test Layers

| Layer | Scope | Runner |
|-------|-------|--------|
| Unit tests | Individual classes (scanner, mutator, log, report, operators) | `FakeTestRunner` — no BC needed |
| Integration tests | Full `MutationPipeline` against a real AL project | Real `AlRunnerPipeline` — requires BC instance |

Run unit tests with: `dotnet test --filter "Category!=Integration"`

## Execution Flow

```
STARTUP
  - Verify clean git working tree (abort if dirty)
  - Load and validate operator definitions

BASELINE
  - Compile + run tests via AL Runner on unmodified code
  - Abort if baseline fails

REPLAY (if mutations.json exists)
  - For each SURVIVED mutation from last run:
    - If original line no longer exists → OBSOLETE
    - Apply → compile → test → restore
    - Record: KILLED or SURVIVED

NEW MUTATIONS
  - Walk NavSyntaxTree for each .al file
  - Match operators to node types, build candidate list
  - Skip candidates already recorded in mutations.json
  - For each candidate: apply → compile → test → restore
  - Record result

REPORT
  - Append run to mutations.json
  - Generate report.md
  - Print summary: score, survivors, required fixes
```

## Mutation Log

Results are persisted to `mutations.json` (append-only, `schema_version: 1`):

- **KILLED** — tests caught the mutation
- **SURVIVED** — tests still passed (test gap)
- **COMPILE_ERROR** — mutation broke compilation (excluded from score)
- **OBSOLETE** — original code no longer exists at that location
