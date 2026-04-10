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
3. For each mutation: modify source → compile → publish → run tests → restore via git
4. Reports which mutations survived (your test gaps)

Mutations are identified using [tree-sitter-al](https://github.com/SShadowS/tree-sitter-al),
a full AST parser for AL. This means mutations only target executable code — never object
properties, attributes, permission sets, or comments.

## Mutation Operators

Operators target specific AST node types:

| Category | Example | What It Tests |
|---|---|---|
| Relational | `>` → `>=` | Boundary conditions |
| Arithmetic | `+` → `-` | Math correctness |
| Logical | `and` → `or` | Condition logic |
| Statement removal | comment out `Rec.Modify(...)` | Side effects are needed |
| BC-specific | `Modify(true)` → `Modify(false)` | Trigger execution |

Custom operators can be defined in JSON. See [Operators Guide](docs/OPERATORS.md).

## Installation

```bash
pip install -e ".[dev]"
```

Requires Python 3.10+.

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
```

See [Usage Guide](docs/USAGE.md) for details.

## Documentation

- [Concept](CONCEPT.md) — Product vision and goals
- [Architecture](docs/ARCHITECTURE.md) — Technical design and execution flow
- [Usage Guide](docs/USAGE.md) — Detailed usage instructions
- [Operators Guide](docs/OPERATORS.md) — Writing custom mutation operators

## Development

```bash
# Create venv and install
python -m venv .venv
source .venv/bin/activate
pip install -e ".[dev]"

# Run tests
pytest -v
```

## License

MIT License — See [LICENSE](LICENSE)
