# Usage Guide

al-mutate is a .NET global tool for mutation testing Business Central AL code.

## Prerequisites

- .NET 8.0 SDK or later
- A BC AL project with source files and a test app
- Clean git working tree (enforced at startup)

## Installation

```bash
dotnet tool install --global MSDyn365BC.AL.Mutate
```

To update to the latest version:

```bash
dotnet tool update --global MSDyn365BC.AL.Mutate
```

## Commands

### scan — List Mutation Candidates

Parses AL files using the NavSyntaxTree and lists all mutation candidates without modifying anything.

```bash
al-mutate scan ./src
al-mutate scan ./src --operators ./my-operators.json
```

Output shows each candidate with its operator, file, line, and the proposed change.

### run — Full Mutation Testing

Runs the complete mutation testing loop: baseline → mutations → report.

```bash
al-mutate run ./src --tests ./test/MyApp.test.app
al-mutate run ./src --tests ./test/MyApp.test.app --max 20
al-mutate run ./src --tests ./test/MyApp.test.app --operators ./my-operators.json
al-mutate run ./src --tests ./test/MyApp.test.app --stubs ./stubs
```

Options:

| Option | Description |
|--------|-------------|
| `--tests <path>` | Path to the compiled test app (.app file) |
| `--max <n>` | Limit to the first N mutations (useful for quick checks) |
| `--operators <path>` | Use a custom operator JSON file instead of the default |
| `--stubs <path>` | Exclude stub AL files from mutation scanning |

This will:
1. Check git working tree is clean
2. Compile + run baseline tests (abort if they fail)
3. For each mutation: apply → compile → test → restore
4. Write `mutations.json` (append-only log) and `report.md`
5. Print summary with mutation score

### replay — Re-test Survived Mutations

Re-tests mutations that survived in a previous run.

```bash
al-mutate replay mutations.json --tests ./test/MyApp.test.app
```

Use this after writing new tests to check if previously-survived mutations are now caught.

## Understanding Results

### Mutation Score

```
Score = Killed / (Killed + Survived) × 100
```

- **Killed** — tests detected the change (good)
- **Survived** — tests missed the change (test gap to fix)
- **Compile Error** — mutation broke compilation (excluded from score)
- **Obsolete** — original code no longer exists at that location

### Output Files

- `mutations.json` — Append-only log of all mutation runs with full details
- `report.md` — Human-readable Markdown report with score, summary, and survived mutation details

## Tips

1. **Start with scan** — Use `al-mutate scan` to see what mutations would be generated
2. **Limit mutations in CI** — Use `--max 50` for faster CI runs
3. **Fix survivors first** — Survived mutations are your biggest test gaps
4. **Replay after fixes** — Use `al-mutate replay` to verify new tests kill previously-survived mutations
5. **Use --stubs for Sentinel-style repos** — Repos with stub AL files should pass `--stubs` to avoid mutating stubs
