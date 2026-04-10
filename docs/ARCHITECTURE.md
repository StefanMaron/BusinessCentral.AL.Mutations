# Architecture

## Overview

al-mutate is a Python CLI tool that performs mutation testing on Business Central AL code.
It parses AL source files using tree-sitter, identifies mutation targets in the AST,
applies mutations one at a time, and runs the test suite to check if each mutation is caught.

## Design Principles

1. **AST-based targeting** — Mutations are identified by querying tree-sitter node types, not text patterns. This eliminates false positives from object properties, attributes, and comments.
2. **Git-based restore** — Mutated files are always restored via `git checkout`, never by rewriting. This guarantees clean state.
3. **Fail fast** — Any restore failure or unexpected error aborts immediately. Never leave mutated code in place.
4. **Append-only log** — Results accumulate across runs in `mutations.json`. Previously-survived mutations can be replayed.

## Project Structure

```
al_mutate/                    # Python package
  __init__.py
  cli.py                      # Entry point: al-mutate command
  scan.py                     # Parse AL with tree-sitter, find mutation targets
  mutate.py                   # Apply mutations to files, restore via git
  run.py                      # Compile, publish, run tests via Linux stack
  operators.py                # Load and validate operator JSON
  report.py                   # Generate Markdown reports
  log.py                      # Mutation log (mutations.json) read/write
operators/
  default.json                # Default AL mutation operators (AST node-type based)
  schema.json                 # JSON Schema for operator files
tests/
  test_scan.py
  test_mutate.py
  test_operators.py
  test_log.py
  test_run.py
  test_report.py
  test_cli.py
  fixtures/
    sample.al                 # Sample AL code for unit tests
    NoMatches.al              # AL code with no mutable constructs
pyproject.toml                # Package definition + CLI entry point
```

## AST-Based Scanning

The scanner uses [tree-sitter-al](https://github.com/SShadowS/tree-sitter-al) to parse
each `.al` file into a full AST. Operators specify which node types to target:

- `comparison_expression` — relational operators (`>`, `<`, `>=`, `<=`, `=`, `<>`)
- `additive_expression` — `+`, `-`
- `multiplicative_expression` — `*`, `/`
- `logical_expression` — `and`, `or`
- `call_expression` — method calls (for statement removal and BC-specific mutations)

Because the AST distinguishes executable code from metadata, comments, and strings,
no separate context filtering is needed.

## Execution Flow

```
STARTUP
  - Verify clean git working tree (abort if dirty)
  - Load and validate operator definitions

BASELINE
  - Compile + publish + run tests on unmodified code
  - Abort if baseline fails

REPLAY (if mutations.json exists)
  - For each SURVIVED mutation from last run:
    - If original line no longer exists → OBSOLETE
    - Apply → compile → publish → test → restore
    - Record: KILLED or SURVIVED

NEW MUTATIONS
  - Parse .al files with tree-sitter
  - Walk AST, match operators to node types
  - For each candidate: apply → compile → publish → test → restore
  - Record result

REPORT
  - Append run to mutations.json
  - Generate report.md
  - Print summary: score, survivors, required fixes
```

## Linux Stack Integration

The tool calls external commands for compile/publish/test:

| Step | Command |
|------|---------|
| Compile | `al-compile` |
| Publish | `bc-publish` |
| Run tests | `/opt/bc-linux/scripts/run-tests.sh` |
| Restore | `git checkout -- <file>` |

These are expected to be available on PATH in the dev container environment.

## Mutation Log

Results are persisted to `mutations.json` (append-only):

- **KILLED** — tests caught the mutation
- **SURVIVED** — tests still passed (test gap)
- **COMPILE_ERROR** — mutation broke compilation (excluded from score)
- **OBSOLETE** — original code no longer exists at that location
