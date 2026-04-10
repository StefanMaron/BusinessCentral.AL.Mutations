# BusinessCentral.AL.Mutations — Development Guide

## Project Pivot: Linux Stack

This project was originally built targeting Windows + BcContainerHelper + PowerShell.
That approach is obsolete. The new stack is:

- **Python CLI** instead of PowerShell module
- **`/opt/bc-linux/scripts/run-tests.sh`** instead of BcContainerHelper for test execution
- **`al-compile` + `bc-publish`** for compile and deploy
- **`git checkout -- <file>`** for mutation restore (requires clean working tree — enforced at startup)
- **Linux-first** — runs inside the same dev container as the AL project

The core concept is identical: apply mutations, run tests, report survivors. Only the runtime changed.

## What This Tool Does

1. Scans `.al` source files for mutable locations using operator definitions
2. Verifies the working tree is clean (git) — aborts if not
3. Runs the baseline test suite — aborts if tests don't pass clean
4. For each mutation: modify source → compile → publish → run tests → restore via git
5. Persists results to `mutations.json` (append-only across runs)
6. On subsequent runs: replays previously-survived mutations first, then generates new ones
7. Reports mutation score, survivors, and required test fixes

## AST-Based Mutation Targeting

Mutation targets are identified using **tree-sitter-al** — a tree-sitter grammar for AL with Python bindings.

```bash
pip install tree-sitter-al    # PyPI package
```

```python
import tree_sitter_al
from tree_sitter import Language, Parser

AL_LANGUAGE = Language(tree_sitter_al.language())
parser = Parser(AL_LANGUAGE)
tree = parser.parse(source_bytes)
```

This gives a full AST. Operators query specific node types — no text pattern matching, no noise from object properties or attributes. The node's position in the tree inherently tells you it's inside a procedure body.

**Key node types for mutation:**

| Node Type | Targets |
|-----------|---------|
| `comparison_expression` | `>`, `<`, `>=`, `<=`, `=`, `<>` |
| `additive_expression` | `+`, `-` |
| `multiplicative_expression` | `*`, `/`, `mod`, `div` |
| `logical_expression` | `and`, `or` |
| `unary_expression` | `not` |
| `call_expression` | method calls (for statement removal) |
| `asserterror_statement` | asserterror (for removal) |

**Operator format** — node-type based instead of text patterns:

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

The grammar is at https://github.com/SShadowS/tree-sitter-al — actively maintained, 100% parse success on 15,358 production AL files, 1,404 tests.

## Project Structure (Target)

```
al_mutate/                    # Python package
  __init__.py
  cli.py                      # Entry point: `al-mutate` command
  scan.py                     # Find mutation targets via tree-sitter AST queries
  mutate.py                   # Apply and restore mutations
  run.py                      # Compile, publish, run tests via Linux stack
  operators.py                # Load and validate operator JSON
  report.py                   # Generate JSON + Markdown reports
  log.py                      # Mutation log (mutations.json) read/write
operators/
  default.json                # Default AL mutation operators (node-type based)
  schema.json                 # JSON Schema for operator files
tests/
  test_scan.py
  test_mutate.py
  test_operators.py
  fixtures/
    sample.al                 # Sample AL snippets for unit tests
pyproject.toml                # Package definition + CLI entry point
README.md
docs/
  ARCHITECTURE.md
  OPERATORS.md
  USAGE.md
```

## CLI Interface

```bash
# Run full mutation test
al-mutate run ./src --tests ./test/MyApp.test.app

# Scan only (list mutations without executing)
al-mutate scan ./src

# Replay previously-survived mutations from an existing log
al-mutate replay mutations.json --tests ./test/MyApp.test.app

# Use custom operators
al-mutate run ./src --tests ./test/MyApp.test.app --operators ./my-operators.json

# Limit mutations (useful for quick checks)
al-mutate run ./src --tests ./test/MyApp.test.app --max 20
```

## Linux Stack Integration

### Compile
```bash
~/.claude/tools/al-smart-compile/al-compile
```

### Publish
```bash
bc-publish    # auto-detects $BC_SERVER
```

Or via curl:
```bash
BC_HOST="${BC_SERVER:-localhost}"
curl -u BCRUNNER:Admin123! -X POST \
  -F "file=@path/to/app.app;type=application/octet-stream" \
  "http://${BC_HOST}:7049/BC/dev/apps?SchemaUpdateMode=forcesync"
```

### Run Tests
```bash
BC_HOST="${BC_SERVER:-localhost}"
/opt/bc-linux/scripts/run-tests.sh \
  --base-url "http://${BC_HOST}:7048/BC" \
  --dev-url "http://${BC_HOST}:7049/BC/dev" \
  --app path/to/test-app.app
```

Exit code: 0 = all pass, 1 = failures exist.

### Mutation Restore
```bash
git checkout -- <file>
```

Always restore via git, never by re-writing the file. If restore fails, abort immediately.

## Mutation Log Format

Mutations are persisted to `mutations.json` (append-only — never delete history):

```json
{
  "schema_version": 1,
  "project": "./src",
  "runs": [
    {
      "run": 1,
      "date": "2026-04-10T09:00:00",
      "mutations": [
        {
          "id": "M001",
          "operator": "rel-gt-to-gte",
          "file": "src/CreditManagement.al",
          "line": 42,
          "original": "if Amount > 0 then",
          "mutated": "if Amount >= 0 then",
          "status": "KILLED",
          "caught_by": "ValidateCreditLimit_Negative_ThrowsError"
        },
        {
          "id": "M002",
          "operator": "stmt-remove-error",
          "file": "src/CreditManagement.al",
          "line": 87,
          "original": "Error('Credit limit exceeded.');",
          "mutated": "// Error('Credit limit exceeded.');",
          "status": "SURVIVED",
          "caught_by": null
        }
      ]
    }
  ]
}
```

**Statuses:** `KILLED` | `SURVIVED` | `COMPILE_ERROR` | `OBSOLETE`

`OBSOLETE` means the `original` string no longer exists in the file — production code changed, mutation needs regenerating for that area.


## Execution Flow

```
STARTUP
  - Verify clean git working tree (abort if dirty)
  - Load operator definitions
  - Check /opt/bc-linux is mounted

BASELINE
  - Compile + publish + run tests on unmodified code
  - Abort if baseline fails (tests must be green before mutation)

REPLAY (if mutations.json exists)
  - For each SURVIVED mutation from last run:
    - Find original string in file (if missing → OBSOLETE)
    - Apply mutation → compile → publish → run tests → restore
    - Record: KILLED (now fixed) or SURVIVED (still a gap)

NEW MUTATIONS
  - Scan .al files for operator matches (skip if identical to existing log entry)
  - For each new candidate:
    - Apply → compile → publish → run tests → restore
    - Record result

REPORT
  - Append new run to mutations.json
  - Generate report.md
  - Print summary: score, survivors, required fixes
```

## Development Principles

**TDD:** Write a failing test first, then implement. Every function must have a test.

**No container dependency in unit tests:** Unit tests use fixture `.al` files and mock the compile/publish/test runner calls. Only integration tests hit a real BC instance.

**Fail fast:** Any unexpected error during mutation (restore failure, compile crash) stops the run immediately. Never leave mutated code in place.

**Tree-sitter handles context:** Because we operate on the AST, mutations are never generated inside comments or string literals — those are distinct node types that operators simply don't target.

## Bootstrap Order

1. `pyproject.toml` — package definition, CLI entry point (`tree-sitter-al` as dependency)
2. `al_mutate/operators.py` + `tests/test_operators.py` — load/validate operator JSON
3. `al_mutate/scan.py` + `tests/test_scan.py` — parse AL with tree-sitter, query node types, return candidates
4. `al_mutate/mutate.py` + `tests/test_mutate.py` — apply and restore mutations
5. `al_mutate/log.py` — read/write mutations.json
6. `al_mutate/run.py` — compile, publish, run tests via Linux stack
7. `al_mutate/report.py` — generate JSON + Markdown
8. `al_mutate/cli.py` — wire it all together into `al-mutate` command
9. `operators/default.json` — full AL operator set (node-type based)

## Current State

The old PowerShell implementation in `BCMutations/` is obsolete. The Python rewrite starts fresh.
The operator definitions in `operators/default.json` (once created) should cover the same
categories as before: relational, arithmetic, logical, boolean, statement-removal, BC-specific.

Files to remove or ignore (old PowerShell code):
- `BCMutations/` directory
- `entrypoint.ps1`
- `action.yml` (GitHub Action — not the priority, Linux CLI is)
- `tests/Unit/`, `tests/Integration/` (Pester tests)
