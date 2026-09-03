# al-mutate (retired)

> **This project is retired and no longer maintained.** Use
> [LethAL](https://github.com/SShadowS/LethAL) instead. The NuGet package
> `MSDyn365BC.AL.Mutate` 0.2.0 stays installable, and it is not getting further releases.

## Why

LethAL does the same job and does it better in the places that decide whether a mutation
testing run on a real AL project is usable at all:

- **One published build carries every mutant**, each one behind a runtime guard, switched on by a
  table write. al-mutate compiles and runs the suite once per mutant, which is what makes it
  impractical past a few hundred mutants. LethAL has been measured against a commercial extension
  with 19,832 mutation sites; al-mutate has no comparable measurement.
- **It mutates a scratch copy**, not your working tree. al-mutate edits your files and restores them
  with `git checkout`, so a crash mid-run leaves mutated source behind.
- **It reports a third verdict, `no-coverage`**, and records how it decided. al-mutate reports only
  killed and survived, so "your test ran this and did not check it" and "no test runs this at all"
  are indistinguishable in its output, and the second one is not a test-quality finding.
- **It refuses to record a verdict it cannot vouch for** — it verifies the build under test is the
  one it compiled, holds a lease so two runs on one server cannot corrupt each other's results, and
  reports an error where al-mutate would report a score.

On mutation operators, al-mutate's 54 count against LethAL's 26 is misleading. 22 of mine are
`stmt-remove-*` variants that LethAL covers with one generic operator, and my six arithmetic
operators are the weaker answer to a problem LethAL measured: AL's `+` is mostly string
concatenation, and only 8.9% of arithmetic sites in a real app are safe to mutate at all.

## What was worth keeping

Two things from here have been offered upstream:

- The run-trigger flag swap on `DeleteAll(true)` and `ModifyAll(true)`, which LethAL does not claim.
- `AlScanner.cs`, which enumerates mutation sites from `Microsoft.Dynamics.Nav.CodeAnalysis` (the AL
  compiler's own parser). LethAL parses with tree-sitter-al, so this is usable as a one-off
  cross-check for grammar blind spots.

Test execution here runs through [AL Runner](https://github.com/StefanMaron/BusinessCentral.AL.Runner),
which is actively maintained and unaffected by this. LethAL can use it as an offline backend.

---

The original documentation follows, unchanged.

## What Is This?

A CLI tool that validates the quality of your AL test suites by introducing small code changes
(mutations) and checking whether your tests catch them.

- **Mutation killed** = your tests detected the change (good)
- **Mutation survived** = your tests missed the change (test gap)

```bash
al-mutate run ./src --tests ./test/MyApp.test.app

# Mutation Score: 75.00% (30 killed, 10 survived)
```

## How It Works

1. Verifies git working tree is clean
2. Runs a baseline compile + test (must pass)
3. For each mutation: modify source → compile → run tests → restore via git
4. Reports which mutations survived (your test gaps)

Mutations are identified using `Microsoft.Dynamics.Nav.CodeAnalysis` — the official AL compiler
SDK — which provides a full syntax tree (NavSyntaxTree). This means mutations only target
executable code — never object properties, attributes, permission sets, or comments.

## Mutation Operators

Operators target specific AST node types:

| Category | Example | What It Tests |
|---|---|---|
| Relational | `>` → `>=` | Boundary conditions |
| Arithmetic | `+` → `-` | Math correctness |
| Logical | `and` → `or` | Condition logic |
| Statement removal | comment out `Rec.Modify(...)` | Side effects are needed |
| BC-specific | `Modify(true)` → `Modify(false)` | Trigger execution |

33 operators across 8 categories. Custom operators can be defined in JSON. See [Operators Guide](docs/OPERATORS.md).

## Installation

The package is not getting further releases. 0.2.0 remains installable:

```bash
dotnet tool install --global MSDyn365BC.AL.Mutate
```

Requires .NET 8.0 SDK or later.

## Usage

```bash
# Scan: list mutation candidates without executing
al-mutate scan ./src

# Run full mutation testing
al-mutate run ./src --tests ./test/MyApp.test.app

# Limit mutations for a quick check
al-mutate run ./src --tests ./test/MyApp.test.app --max 20

# Replay previously-survived mutations
al-mutate replay mutations.json --tests ./test/MyApp.test.app

# Use custom operators
al-mutate run ./src --tests ./test/MyApp.test.app --operators ./my-operators.json

# Exclude stub files (for repos like Sentinel)
al-mutate run ./src --tests ./test/MyApp.test.app --stubs ./stubs
```

See [Usage Guide](docs/USAGE.md) for details.

## Documentation

- [Concept](CONCEPT.md) — Product vision and goals
- [Architecture](docs/ARCHITECTURE.md) — Technical design and execution flow
- [Usage Guide](docs/USAGE.md) — Detailed usage instructions
- [Operators Guide](docs/OPERATORS.md) — Writing custom mutation operators

## Development

```bash
# Build
dotnet build AlMutate/AlMutate.slnx

# Run unit tests (no BC instance required)
dotnet test AlMutate/AlMutate.slnx --filter "Category!=Integration"

# Run all tests
dotnet test AlMutate/AlMutate.slnx
```

Test execution uses [AL Runner](https://github.com/StefanMaron/BusinessCentral.AL.Runner) for
in-process test execution — no BC container or Linux stack tools required.

## Project Structure

```
AlMutate/
  AlMutate.slnx               # Solution file
  AlMutate/                   # Main tool project (C#)
  AlMutate.Tests/             # xUnit test project
operators/
  default.json                # Default AL mutation operators
docs/
  ARCHITECTURE.md
  OPERATORS.md
  USAGE.md
```

## License

MIT License — See [LICENSE](LICENSE)
