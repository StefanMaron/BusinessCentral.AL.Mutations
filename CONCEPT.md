# BusinessCentral.AL.Mutations - Product Concept

## Vision

A mutation testing tool for Microsoft Dynamics 365 Business Central AL code.
Validate the quality of your AL test suites by introducing small code changes (mutations)
and verifying that your tests catch them.

## Problem Statement

Business Central developers write tests but have no way to measure test quality beyond code coverage:

- Code coverage tells you what code is executed, not what is actually tested
- Tests can execute code without meaningful assertions
- Missing edge case tests go undetected
- Removed or changed business logic may not break any test

## What Is Mutation Testing?

Mutation testing introduces small, deliberate changes to your source code (mutants) and runs
your test suite against each one. If your tests catch the change (fail), the mutant is "killed"
and your tests are effective. If your tests still pass despite the change, the mutant "survived"
and you have a gap in your test coverage.

Example: If your code has `if Amount > 0 then` and a mutation changes it to `if Amount >= 0 then`,
your tests should fail. If they don't, your tests never exercise the boundary condition.

## Solution

```bash
# Scan for mutation candidates (dry run)
al-mutate scan ./src

# Run full mutation testing
al-mutate run ./src --tests ./test/MyApp.test.app

# Output:
# Mutation Score: 75.00% (30 killed, 10 survived)
# Report written to report.md
```

The tool:
1. Parses AL source files using tree-sitter (full AST — no text pattern noise)
2. Compiles and tests your unmodified code (baseline)
3. For each mutation: modify source → compile → deploy → test → restore via git
4. Reports which mutations survived (test gaps)

## Core Features

1. **AST-based targeting** — Uses tree-sitter-al to parse AL code, targeting specific node types. No false positives from object properties, attributes, or comments.
2. **Configurable operators** — Mutation operators defined in JSON targeting AST node types
3. **Context-free by design** — Tree-sitter inherently distinguishes executable code from metadata
4. **Append-only mutation log** — Results persist across runs in `mutations.json`
5. **Replay survivors** — Re-test previously-survived mutations after writing new tests
6. **CLI tool** — Run locally during development

## Mutation Operator Categories

| Category | Example | What It Tests |
|---|---|---|
| Relational | `>` to `>=` | Boundary conditions |
| Arithmetic | `+` to `-` | Math correctness |
| Logical | `and` to `or` | Condition logic |
| Statement removal | comment out `Modify(...)` | Side effects are needed |
| BC-specific | `Modify(true)` to `Modify(false)` | Trigger execution |

Operators target tree-sitter node types:
```json
{
  "operators": [
    {
      "id": "rel-gt-to-gte",
      "category": "relational",
      "node_type": "comparison_expression",
      "operator_token": ">",
      "replacement": ">="
    }
  ]
}
```

## Technical Approach

- **Python CLI** (`al-mutate` command)
- **tree-sitter-al** for AST parsing — no text pattern matching
- **`al-compile` + `bc-publish`** for compile and deploy
- **`/opt/bc-linux/scripts/run-tests.sh`** for test execution
- **`git checkout -- <file>`** for mutation restore
- **pytest** for testing the tool itself

## Success Criteria

- [x] Python CLI with `al-mutate` command
- [x] Default operator set covering relational, arithmetic, logical, statement removal, BC-specific
- [x] AST-based targeting via tree-sitter (replaces text pattern matching)
- [x] Markdown report output
- [x] Mutation log with replay support
- [x] pytest test suite for the tool
- [ ] Documentation with usage examples
- [ ] CI workflow

## Non-Goals (v1)

- Parallel mutation execution (single-threaded loop is fine for v1)
- HTML report viewer (Markdown covers reporting needs)
- Equivalent mutant detection (report all survivors, let user decide)
