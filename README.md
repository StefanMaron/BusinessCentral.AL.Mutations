# al-mutate

Mutation testing for Microsoft Dynamics 365 Business Central AL code.

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
