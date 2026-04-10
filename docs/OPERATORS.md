# Mutation Operators

Mutation operators define what code changes al-mutate will introduce into your AL source files.
Operators target specific tree-sitter AST node types, ensuring mutations only apply to
executable code.

## Default Operators

al-mutate ships with a comprehensive set of default operators.

### Relational Operators

Target: `comparison_expression` nodes. Test boundary conditions.

| ID | Token | Replacement | Tests |
|----|-------|-------------|-------|
| `rel-gt-to-gte` | `>` | `>=` | Off-by-one at boundaries |
| `rel-gte-to-gt` | `>=` | `>` | Off-by-one at boundaries |
| `rel-lt-to-lte` | `<` | `<=` | Off-by-one at boundaries |
| `rel-lte-to-lt` | `<=` | `<` | Off-by-one at boundaries |
| `rel-eq-to-neq` | `=` | `<>` | Equality checks |
| `rel-neq-to-eq` | `<>` | `=` | Inequality checks |

### Arithmetic Operators

Target: `additive_expression` and `multiplicative_expression` nodes.

| ID | Token | Replacement | Tests |
|----|-------|-------------|-------|
| `arith-add-to-sub` | `+` | `-` | Addition logic |
| `arith-sub-to-add` | `-` | `+` | Subtraction logic |
| `arith-mul-to-div` | `*` | `/` | Multiplication logic |
| `arith-div-to-mul` | `/` | `*` | Division logic |

### Logical Operators

Target: `logical_expression` nodes.

| ID | Token | Replacement | Tests |
|----|-------|-------------|-------|
| `logic-and-to-or` | `and` | `or` | Compound conditions |
| `logic-or-to-and` | `or` | `and` | Compound conditions |

### Statement Removal

Target: `call_expression` nodes. Comments out the entire statement.

| ID | Method | Tests |
|----|--------|-------|
| `stmt-remove-modify` | `Modify` | Record modifications are needed |
| `stmt-remove-insert` | `Insert` | Record insertions are needed |
| `stmt-remove-delete` | `Delete` | Record deletions are needed |
| `stmt-remove-validate` | `Validate` | Field validations are needed |
| `stmt-remove-error` | `Error` | Error handling is needed |

### BC-Specific Operators

Target: `call_expression` nodes with specific argument values.

| ID | Call | Replacement | Tests |
|----|------|-------------|-------|
| `bc-modify-trigger-true` | `Modify(true)` | `Modify(false)` | Trigger execution |
| `bc-modify-trigger-false` | `Modify(false)` | `Modify(true)` | Trigger suppression |
| `bc-insert-trigger-true` | `Insert(true)` | `Insert(false)` | Trigger execution |
| `bc-delete-trigger-true` | `Delete(true)` | `Delete(false)` | Trigger execution |

## Why AST-Based?

Previous text-based pattern matching (e.g., searching for ` > ` in raw text) generates
excessive noise — matching object properties, permission sets, attributes, and other
non-executable constructs. In a real project, this produced 1193 candidates where only
193 were actual executable code.

Tree-sitter parses the full AST, so operators only match nodes that exist inside
procedure and trigger bodies. Comments, strings, and metadata are distinct node types
that operators never target.

## Writing Custom Operators

Create a JSON file following the operator schema:

```json
{
  "$schema": "./schema.json",
  "operators": [
    {
      "id": "my-custom-op",
      "name": "Description of what this tests",
      "category": "relational",
      "node_type": "comparison_expression",
      "operator_token": ">",
      "replacement": ">="
    },
    {
      "id": "remove-my-procedure",
      "name": "Remove MyProcedure call",
      "category": "statement-removal",
      "node_type": "call_expression",
      "identifier": "MyProcedure",
      "replacement": null
    },
    {
      "id": "my-bc-specific",
      "name": "MyProc(true) to MyProc(false)",
      "category": "bc-specific",
      "node_type": "call_expression",
      "identifier": "MyProc",
      "argument_match": "true",
      "replacement": "false"
    }
  ]
}
```

### Field Reference

| Field | Required | Description |
|-------|----------|-------------|
| `id` | Yes | Unique kebab-case identifier |
| `name` | Yes | Human-readable description |
| `category` | Yes | One of: `relational`, `arithmetic`, `logical`, `boolean`, `statement-removal`, `boundary`, `control-flow`, `bc-specific` |
| `node_type` | Yes | Tree-sitter node type to target |
| `operator_token` | No | Operator text to match within the node (for expression operators) |
| `identifier` | No | Method name to match (for `call_expression` nodes) |
| `argument_match` | No | Argument value to match (for BC-specific argument swaps) |
| `replacement` | No | Text to substitute. `null` comments out the entire line. |

### Using Custom Operators

```bash
al-mutate scan ./src --operators ./my-operators.json
al-mutate run ./src --tests ./test/MyApp.test.app --operators ./my-operators.json
```
