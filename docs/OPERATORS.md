# Mutation Operators

Mutation operators define what code changes (mutations) BCMutations will introduce into your AL source files.

## Default Operators

BCMutations ships with a comprehensive set of default operators covering the most common mutation categories.

### Relational Operators

These test boundary conditions in your AL code.

| ID | Pattern | Replacement | Tests |
|----|---------|-------------|-------|
| `rel-gt-to-gte` | ` > ` | ` >= ` | Off-by-one at boundaries |
| `rel-gte-to-gt` | ` >= ` | ` > ` | Off-by-one at boundaries |
| `rel-lt-to-lte` | ` < ` | ` <= ` | Off-by-one at boundaries |
| `rel-lte-to-lt` | ` <= ` | ` < ` | Off-by-one at boundaries |
| `rel-eq-to-neq` | ` = ` | ` <> ` | Equality checks |
| `rel-neq-to-eq` | ` <> ` | ` = ` | Inequality checks |

### Arithmetic Operators

These test mathematical correctness.

| ID | Pattern | Replacement | Tests |
|----|---------|-------------|-------|
| `arith-add-to-sub` | ` + ` | ` - ` | Addition logic |
| `arith-sub-to-add` | ` - ` | ` + ` | Subtraction logic |
| `arith-mul-to-div` | ` * ` | ` / ` | Multiplication logic |
| `arith-div-to-mul` | ` / ` | ` * ` | Division logic |

### Logical Operators

These test condition logic.

| ID | Pattern | Replacement | Tests |
|----|---------|-------------|-------|
| `logic-and-to-or` | ` and ` | ` or ` | Compound conditions |
| `logic-or-to-and` | ` or ` | ` and ` | Compound conditions |

### Boolean Operators

These test flag handling.

| ID | Pattern | Replacement | Tests |
|----|---------|-------------|-------|
| `bool-true-to-false` | `true` | `false` | Boolean flags |
| `bool-false-to-true` | `false` | `true` | Boolean flags |

### Statement Removal

These test that important side effects (writes, validations) are actually needed.
When replacement is `null`, the entire line is commented out.

| ID | Pattern | Replacement | Tests |
|----|---------|-------------|-------|
| `stmt-remove-modify` | `.Modify(` | (line commented out) | Record modifications are needed |
| `stmt-remove-insert` | `.Insert(` | (line commented out) | Record insertions are needed |
| `stmt-remove-delete` | `.Delete(` | (line commented out) | Record deletions are needed |
| `stmt-remove-validate` | `.Validate(` | (line commented out) | Field validations are needed |

### Boundary Operators

These test off-by-one errors in numeric calculations.

| ID | Pattern | Replacement | Tests |
|----|---------|-------------|-------|
| `boundary-off-by-one-plus` | ` + 1` | ` + 2` | Exact boundary values |
| `boundary-off-by-one-minus` | ` - 1` | ` - 2` | Exact boundary values |

### Control Flow Operators

These test conditional logic.

| ID | Pattern | Replacement | Tests |
|----|---------|-------------|-------|
| `ctrl-if-negate` | `if ` | `if not ` | Conditions are correct |

### BC-Specific Operators

These test Business Central-specific patterns.

| ID | Pattern | Replacement | Tests |
|----|---------|-------------|-------|
| `bc-modify-trigger-true` | `.Modify(true)` | `.Modify(false)` | Trigger execution on modify |
| `bc-modify-trigger-false` | `.Modify(false)` | `.Modify(true)` | Trigger suppression on modify |
| `bc-insert-trigger-true` | `.Insert(true)` | `.Insert(false)` | Trigger execution on insert |
| `bc-delete-trigger-true` | `.Delete(true)` | `.Delete(false)` | Trigger execution on delete |

## Context Filtering

The engine automatically skips mutations inside:
- **Single-line comments**: `// ... `
- **Block comments**: `/* ... */`
- **String literals**: `'...'`

You don't need to worry about these in operator definitions.

## Writing Custom Operators

Create a JSON file following the operator schema:

```json
{
  "$schema": "https://raw.githubusercontent.com/StefanMaron/BusinessCentral.AL.Mutations/master/operators/schema.json",
  "operators": [
    {
      "id": "my-custom-operator",
      "name": "Human readable description",
      "category": "relational",
      "pattern": " > ",
      "replacement": " >= "
    },
    {
      "id": "remove-my-procedure",
      "name": "Remove MyProcedure call",
      "category": "statement-removal",
      "pattern": "MyProcedure(",
      "replacement": null
    }
  ]
}
```

### Field Reference

| Field | Required | Description |
|-------|----------|-------------|
| `id` | Yes | Unique kebab-case identifier (e.g., `rel-gt-to-gte`) |
| `name` | Yes | Human-readable description |
| `category` | Yes | One of: `relational`, `arithmetic`, `logical`, `boolean`, `statement-removal`, `boundary`, `control-flow`, `bc-specific` |
| `pattern` | Yes | Literal string to find in AL source lines |
| `replacement` | No | String to substitute. `null` means comment out the entire line. |

### Tips for Writing Operators

1. **Include spaces in patterns**: Use ` > ` (with spaces) instead of `>` to avoid false matches in strings like `->` or `>=`
2. **Use null for statement removal**: Setting `replacement: null` comments out the whole line, which is safer than trying to remove just part of it
3. **Be specific**: More specific patterns reduce false positives
4. **Test with dry run**: Use `-DryRun` to see what your operators match before running

### Using Custom Operators

```powershell
# Use a custom operator file
Invoke-BCMutationTest -ProjectPath ./MyBCExtension -OperatorFile ./my-operators.json

# View operators from a custom file
Get-BCMutationOperators -OperatorFile ./my-operators.json
```
