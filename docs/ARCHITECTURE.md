# Architecture

## Design Principles

1. **Fluent Interface** - All methods return `Self` for chaining
2. **Immutable Operations** - Each chain step creates intent, Apply() executes
3. **Type Safety** - Leverage AL's type system for compile-time checks
4. **Minimal Overhead** - Keep runtime cost low

## Core Components

### IMutation Interface

```al
interface "SMC IMutation"
{
    procedure Set(FieldNo: Integer; Value: Variant): Interface "SMC IMutation";
    procedure Validate(FieldNo: Integer; Value: Variant): Interface "SMC IMutation";
    procedure When(Condition: Boolean): Interface "SMC IMutation";
    procedure Apply(): Boolean;
}
```

### Mutation Base Codeunit

Handles the generic mutation logic:
- Field change tracking
- Conditional execution
- Apply logic

### Entity-Specific Codeunits

Thin wrappers providing typed access:
- CustomerMutation
- VendorMutation
- ItemMutation
- etc.

## Data Flow

```
User Code
    │
    ▼
Mutate.Customer(No)     ──► Creates CustomerMutation instance
    │
    ▼
.Set(Name, 'Value')     ──► Queues field change
    │
    ▼
.Validate(Field, Val)   ──► Queues validation
    │
    ▼
.Apply()                ──► Executes all changes atomically
    │
    ▼
Returns Boolean         ──► Success/failure
```

## Field Change Tracking

Changes are stored in a temporary table or dictionary:

```al
// Pseudo-structure
FieldChanges: Dictionary of [Integer, Variant]
ValidateFields: Dictionary of [Integer, Variant]
Conditions: List of [Boolean]
```

## Error Handling

- `Apply()` returns Boolean for simple success/failure
- Detailed errors via `GetLastError()` method
- Optional strict mode that throws on failure

## Future Considerations

- Batch operations across multiple records
- Transaction support
- Change auditing
- Event hooks (OnBeforeApply, OnAfterApply)
