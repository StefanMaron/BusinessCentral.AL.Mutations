# API Documentation

## Overview

BusinessCentral.AL.Mutations provides a fluent, chainable API for modifying Business Central records. This document describes the available interfaces, codeunits, and methods.

## Core Interface

### SMC IMutation

The base interface for all mutation operations.

```al
interface "SMC IMutation"
{
    procedure Set(FieldNo: Integer; Value: Variant);
    procedure Validate(FieldNo: Integer; Value: Variant);
    procedure When(Condition: Boolean);
    procedure Apply(): Boolean;
}
```

#### Methods

**Set(FieldNo: Integer; Value: Variant)**
- Sets a field value without validation
- Parameters:
  - `FieldNo`: The field number to modify
  - `Value`: The value to assign
- Returns: void (chainable)

**Validate(FieldNo: Integer; Value: Variant)**
- Sets a field value with validation (calls the field's OnValidate trigger)
- Parameters:
  - `FieldNo`: The field number to validate
  - `Value`: The value to validate and assign
- Returns: void (chainable)

**When(Condition: Boolean)**
- Conditionally applies subsequent operations
- If `Condition` is false, subsequent operations until the next `When()` or `Apply()` are skipped
- Parameters:
  - `Condition`: Boolean condition to evaluate
- Returns: void (chainable)

**Apply(): Boolean**
- Applies all pending mutations to the record
- Calls `Modify(true)` on the underlying record
- Returns: `true` if successful, `false` otherwise

## Base Implementation

### SMC Mutation Base (Codeunit 50100)

Base implementation of the `SMC IMutation` interface. Handles the core mutation logic using RecordRef.

#### Public Methods

**Initialize(RecRef: RecordRef)**
- Initializes the mutation with a record reference
- Must be called before any other operations
- Parameters:
  - `RecRef`: The RecordRef pointing to the record to mutate

**GetRecordRef(): RecordRef**
- Returns the underlying RecordRef
- Useful for testing or advanced scenarios
- Returns: The RecordRef being mutated

## Entity-Specific Implementations

### SMC Customer Mutation (Codeunit 50101)

Customer-specific mutation implementation providing type-safe methods for common Customer fields.

#### Initialization

**Init(CustomerNo: Code[20])**
- Initializes the mutation for a specific customer
- Gets the customer record and prepares it for mutation
- Parameters:
  - `CustomerNo`: The customer number to mutate
- Throws: Error if customer not found

#### Type-Safe Field Methods

**SetName(Name: Text[100]): Codeunit "SMC Customer Mutation"**
- Sets the customer name (Field No. 2)
- Parameters:
  - `Name`: The name to set
- Returns: Self for chaining

**SetAddress(Address: Text[100]): Codeunit "SMC Customer Mutation"**
- Sets the customer address (Field No. 5)
- Parameters:
  - `Address`: The address to set
- Returns: Self for chaining

**SetCity(City: Text[30]): Codeunit "SMC Customer Mutation"**
- Sets the customer city (Field No. 7)
- Parameters:
  - `City`: The city to set
- Returns: Self for chaining

#### Generic Field Methods

**ValidateField(FieldNo: Integer; Value: Variant): Codeunit "SMC Customer Mutation"**
- Validates and sets any field by field number
- Parameters:
  - `FieldNo`: The field number to validate
  - `Value`: The value to validate and set
- Returns: Self for chaining

**When(Condition: Boolean): Codeunit "SMC Customer Mutation"**
- Conditionally applies subsequent operations
- Parameters:
  - `Condition`: Boolean condition to evaluate
- Returns: Self for chaining

**Apply(): Boolean**
- Applies all pending mutations to the customer record
- Returns: `true` if successful, `false` otherwise

## Usage Examples

### Basic Field Update

```al
var
    CustomerMutation: Codeunit "SMC Customer Mutation";
begin
    CustomerMutation.Init('CUSTOMER001');
    CustomerMutation.SetName('New Customer Name');
    CustomerMutation.Apply();
end;
```

### Chained Operations

```al
var
    CustomerMutation: Codeunit "SMC Customer Mutation";
    Success: Boolean;
begin
    Success := CustomerMutation.Init('CUSTOMER001')
        .SetName('ACME Corporation')
        .SetAddress('123 Main Street')
        .SetCity('Seattle')
        .Apply();

    if not Success then
        Error('Failed to update customer');
end;
```

### Conditional Updates

```al
var
    CustomerMutation: Codeunit "SMC Customer Mutation";
    UpdateAddress: Boolean;
begin
    UpdateAddress := true; // Business logic determines this

    CustomerMutation.Init('CUSTOMER001');
    CustomerMutation.SetName('Updated Name');
    CustomerMutation.When(UpdateAddress)
        .SetAddress('456 Oak Avenue')
        .SetCity('Portland');
    CustomerMutation.Apply();
end;
```

### Using Validation

```al
var
    CustomerMutation: Codeunit "SMC Customer Mutation";
    Customer: Record Customer;
begin
    // Validate payment terms code (triggers OnValidate)
    CustomerMutation.Init('CUSTOMER001');
    CustomerMutation.ValidateField(Customer.FieldNo("Payment Terms Code"), '30 DAYS');
    CustomerMutation.Apply();
end;
```

## Error Handling

All mutation operations follow a two-phase approach:

1. **Build Phase**: Calls to `Set()`, `Validate()`, and `When()` queue operations but don't modify the record
2. **Apply Phase**: The `Apply()` method executes all queued operations atomically

If `Apply()` fails, the record is not modified. Check the return value of `Apply()` to determine success:

```al
if not CustomerMutation.Apply() then
    Error('Failed to apply mutations: %1', GetLastErrorText());
```

## Extension Points

### Creating New Entity Mutations

To create a mutation for a new entity (e.g., Vendor, Item):

1. Create a new codeunit with a descriptive name (e.g., "SMC Vendor Mutation")
2. Add a variable of type `Codeunit "SMC Mutation Base"`
3. Implement an `Init()` method that gets the record and calls `MutationBase.Initialize()`
4. Add type-safe methods for common fields
5. Add wrapper methods for `When()` and `Apply()` that return `Self`

Example:

```al
codeunit 50102 "SMC Vendor Mutation"
{
    var
        MutationBase: Codeunit "SMC Mutation Base";
        VendorRec: Record Vendor;

    procedure Init(VendorNo: Code[20])
    var
        RecRef: RecordRef;
    begin
        VendorRec.Get(VendorNo);
        RecRef.GetTable(VendorRec);
        MutationBase.Initialize(RecRef);
    end;

    procedure SetName(Name: Text[100]): Codeunit "SMC Vendor Mutation"
    begin
        MutationBase.Set(VendorRec.FieldNo(Name), Name);
        exit(this);
    end;

    procedure Apply(): Boolean
    var
        RecRef: RecordRef;
    begin
        RecRef := MutationBase.GetRecordRef();
        RecRef.SetTable(VendorRec);
        exit(MutationBase.Apply());
    end;
}
```

## Version Compatibility

- **Platform Version**: 26.0.0.0+
- **Runtime Version**: 14.0+
- **Target**: Cloud

## Object ID Ranges

- Core Framework: 50100-50109
- Entity Mutations: 50110-50149
- Tests: 50150-50199
