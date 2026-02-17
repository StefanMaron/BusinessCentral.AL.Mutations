# BusinessCentral.AL.Mutations

Mutation testing for Microsoft Dynamics 365 Business Central AL code.

> **Note**: This project is developed autonomously by Claude. Create an issue to request features or report bugs!

## What Is This?

A tool that validates the quality of your AL test suites by introducing small code changes
(mutations) and checking whether your tests catch them.

- **Mutation killed** = your tests detected the change (good)
- **Mutation survived** = your tests missed the change (test gap)

```powershell
# Run mutation testing on your AL project
Invoke-BCMutationTest -ProjectPath ./MyBCExtension

# Mutation Score: 75.0% (30 killed, 10 survived)
```

## How It Works

1. Spins up a BC container (once)
2. Compiles and tests your unmodified code (baseline must pass)
3. For each mutation: modify source -> compile -> deploy -> test -> restore
4. Reports which mutations survived (your test gaps)
5. Cleans up the container

## Mutation Operators

Operators are defined as simple JSON token pairs:

| Category | Example | What It Tests |
|---|---|---|
| Relational | `>` to `>=` | Boundary conditions |
| Arithmetic | `+` to `-` | Math correctness |
| Logical | `and` to `or` | Condition logic |
| Boolean | `true` to `false` | Flag handling |
| Statement removal | delete `Modify(...)` | Side effects are needed |
| BC-specific | `Modify(true)` to `Modify(false)` | Trigger execution |

Custom operators can be defined in a JSON file. See [Operators Guide](docs/OPERATORS.md).

## Usage

### CLI

```powershell
Import-Module ./BCMutations

# Full run
Invoke-BCMutationTest -ProjectPath ./MyBCExtension

# Dry run (list mutations without executing)
Invoke-BCMutationTest -ProjectPath ./MyBCExtension -DryRun

# Custom operators and report format
Invoke-BCMutationTest -ProjectPath ./MyBCExtension -OperatorFile ./my-operators.json -ReportFormat markdown
```

### GitHub Action

```yaml
- uses: StefanMaron/BusinessCentral.AL.Mutations@v1
  with:
    project-path: '.'
    report-format: 'markdown'
```

## Documentation

- [Concept](CONCEPT.md) - Product vision and goals
- [Architecture](docs/ARCHITECTURE.md) - Technical design and execution flow
- [Usage Guide](docs/USAGE.md) - Detailed usage instructions
- [Operators Guide](docs/OPERATORS.md) - Writing custom mutation operators

## Current Status

**In development** - This project is being rebuilt as a PowerShell mutation testing tool.

## Contributing

This project uses an AI-first development model:

1. **Create an Issue** - Describe what you want
2. **Claude Implements** - The AI developer picks it up
3. **Review PR** - Human reviews and approves
4. **Merged!** - Feature ships

Mention `@claude` in an issue or comment to get Claude's attention.

## License

MIT License - See [LICENSE](LICENSE)

## Author

Created by Stefan Maron, developed by Claude.
