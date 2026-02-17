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

```powershell
# Run mutation testing on your AL project
Invoke-BCMutationTest -ProjectPath ./MyBCExtension

# Output:
# Mutation Score: 75.0% (30 killed, 10 survived)
# See mutation-report/report.json for details
```

The tool:
1. Spins up a BC container (once)
2. Compiles and tests your unmodified code (baseline)
3. For each mutation: modify source -> compile -> deploy -> test -> restore
4. Reports which mutations survived (test gaps)
5. Cleans up the container

## Core Features

1. **Configurable Operators** - Mutation operators defined in JSON as simple token pairs
2. **Single Container Loop** - One expensive container creation, fast mutation iterations
3. **Context-Aware** - Skips mutations inside comments and string literals
4. **Multiple Report Formats** - JSON, Markdown, HTML
5. **GitHub Action** - Run mutation testing in CI/CD pipelines
6. **CLI Tool** - Run locally during development
7. **Dry Run Mode** - List mutations without executing

## Mutation Operator Categories

| Category | Example | What It Tests |
|---|---|---|
| Relational | `>` to `>=` | Boundary conditions |
| Arithmetic | `+` to `-` | Math correctness |
| Logical | `and` to `or` | Condition logic |
| Boolean | `true` to `false` | Flag handling |
| Statement removal | delete `Modify(...)` | Side effects are needed |
| BC-specific | `Modify(true)` to `Modify(false)` | Trigger execution |

Operators are defined as simple JSON token pairs:
```json
{
  "operators": [
    { "id": "rel-gt-to-gte", "category": "relational", "pattern": " > ", "replacement": " >= " },
    { "id": "stmt-remove-modify", "category": "statement-removal", "pattern": ".Modify(", "replacement": null }
  ]
}
```

## Technical Approach

- PowerShell module (fits BC ecosystem, BcContainerHelper is PowerShell)
- BcContainerHelper for container lifecycle, compilation, deployment, testing
- Pester for testing the tool itself
- GitHub Action (composite) for CI/CD integration

## Success Criteria

- [ ] PowerShell module with `Invoke-BCMutationTest` command
- [ ] Default operator set covering relational, arithmetic, logical, boolean, statement removal, BC-specific
- [ ] Context filtering (skip comments and strings)
- [ ] JSON + Markdown report output
- [ ] GitHub Action wrapper
- [ ] Pester test suite for the tool
- [ ] Documentation with usage examples

## Non-Goals (v1)

- Full AL parser (simple context detection is sufficient)
- Parallel mutation execution (single-threaded loop is fine for v1)
- HTML report viewer (JSON + Markdown covers reporting needs)
- Equivalent mutant detection (report all survivors, let user decide)
