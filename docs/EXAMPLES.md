# Usage Examples

This document provides practical examples of using BusinessCentral.AL.Mutations in real-world scenarios.

## Table of Contents

1. [Basic Customer Updates](#basic-customer-updates)
2. [Conditional Updates](#conditional-updates)
3. [Validation Scenarios](#validation-scenarios)
4. [Bulk Operations](#bulk-operations)
5. [Error Handling](#error-handling)
6. [Integration Patterns](#integration-patterns)

## Basic Customer Updates

### Single Field Update

The simplest use case - updating a single field on a customer record.

```al
procedure UpdateCustomerName(CustomerNo: Code[20]; NewName: Text[100])
var
    CustomerMutation: Codeunit "SMC Customer Mutation";
begin
    CustomerMutation.Init(CustomerNo);
    CustomerMutation.SetName(NewName);

    if not CustomerMutation.Apply() then
        Error('Failed to update customer name');
end;
```

### Multiple Field Updates (Traditional vs Mutation)

**Traditional Approach:**
```al
procedure UpdateCustomerTraditional(CustomerNo: Code[20])
var
    Customer: Record Customer;
begin
    Customer.Get(CustomerNo);
    Customer.Name := 'ACME Corporation';
    Customer.Address := '123 Main Street';
    Customer.City := 'Seattle';
    Customer."Post Code" := '98101';
    Customer.Modify(true);
end;
```

**Using Mutations:**
```al
procedure UpdateCustomerWithMutation(CustomerNo: Code[20])
var
    CustomerMutation: Codeunit "SMC Customer Mutation";
begin
    CustomerMutation.Init(CustomerNo);
    CustomerMutation
        .SetName('ACME Corporation')
        .SetAddress('123 Main Street')
        .SetCity('Seattle')
        .Apply();
end;
```

### Fluent Chaining with Result Check

```al
procedure UpdateCustomerProfile(CustomerNo: Code[20]): Boolean
var
    CustomerMutation: Codeunit "SMC Customer Mutation";
begin
    exit(
        CustomerMutation.Init(CustomerNo)
            .SetName('Global Imports Ltd')
            .SetAddress('456 Commerce Blvd')
            .SetCity('Portland')
            .Apply()
    );
end;
```

## Conditional Updates

### Simple Conditional Field

Update a field only if a condition is met.

```al
procedure UpdateCustomerConditionally(CustomerNo: Code[20]; ShouldUpdateAddress: Boolean)
var
    CustomerMutation: Codeunit "SMC Customer Mutation";
begin
    CustomerMutation.Init(CustomerNo);
    CustomerMutation.SetName('Updated Name');
    CustomerMutation
        .When(ShouldUpdateAddress)
        .SetAddress('New Address')
        .SetCity('New City');
    CustomerMutation.Apply();
end;
```

### Multiple Conditional Blocks

```al
procedure UpdateCustomerBasedOnType(CustomerNo: Code[20]; CustomerType: Option Domestic,International,VIP)
var
    CustomerMutation: Codeunit "SMC Customer Mutation";
begin
    CustomerMutation.Init(CustomerNo);

    // Common update
    CustomerMutation.SetName('Updated Customer');

    // Conditional updates based on type
    CustomerMutation
        .When(CustomerType = CustomerType::International)
        .SetAddress('International Office Address');

    CustomerMutation
        .When(CustomerType = CustomerType::VIP)
        .ValidateField(18, 'VIP-TERMS'); // Payment Terms Code

    if not CustomerMutation.Apply() then
        Error('Failed to update customer');
end;
```

### Business Logic Conditions

```al
procedure UpdateCustomerCreditLimit(CustomerNo: Code[20]; NewLimit: Decimal)
var
    CustomerMutation: Codeunit "SMC Customer Mutation";
    Customer: Record Customer;
    ShouldApprove: Boolean;
begin
    Customer.Get(CustomerNo);

    // Business logic: auto-approve if under 10000, otherwise needs approval
    ShouldApprove := NewLimit < 10000;

    CustomerMutation.Init(CustomerNo);
    CustomerMutation
        .When(ShouldApprove)
        .ValidateField(Customer.FieldNo("Credit Limit (LCY)"), NewLimit);

    if not CustomerMutation.Apply() then begin
        if not ShouldApprove then
            Message('Credit limit change requires approval')
        else
            Error('Failed to update credit limit');
    end;
end;
```

## Validation Scenarios

### Using Field Validation

Trigger the OnValidate logic for fields that have business rules.

```al
procedure UpdatePaymentTerms(CustomerNo: Code[20]; PaymentTermsCode: Code[10])
var
    CustomerMutation: Codeunit "SMC Customer Mutation";
    Customer: Record Customer;
begin
    // Using Validate ensures the Payment Terms OnValidate trigger fires
    // which may update related fields like Payment Method Code, etc.
    CustomerMutation.Init(CustomerNo);
    CustomerMutation.ValidateField(Customer.FieldNo("Payment Terms Code"), PaymentTermsCode);

    if not CustomerMutation.Apply() then
        Error('Invalid payment terms code or failed to update');
end;
```

### Mixed Set and Validate

Some fields need validation, others don't.

```al
procedure UpdateCustomerComplete(CustomerNo: Code[20])
var
    CustomerMutation: Codeunit "SMC Customer Mutation";
    Customer: Record Customer;
begin
    CustomerMutation.Init(CustomerNo);

    // Simple field updates (no validation needed)
    CustomerMutation
        .SetName('Updated Corporation')
        .SetAddress('789 Business Park')
        .SetCity('Denver');

    // Fields that need validation
    CustomerMutation
        .ValidateField(Customer.FieldNo("Payment Terms Code"), '30NET')
        .ValidateField(Customer.FieldNo("Currency Code"), 'USD');

    if not CustomerMutation.Apply() then
        Error('Failed to update customer: %1', GetLastErrorText());
end;
```

## Bulk Operations

### Processing Multiple Records

Update multiple customers with the same pattern.

```al
procedure UpdateMultipleCustomers(var CustomerFilter: Record Customer; NewCity: Text[30])
var
    CustomerMutation: Codeunit "SMC Customer Mutation";
    Customer: Record Customer;
    UpdateCount: Integer;
    FailCount: Integer;
begin
    Customer.Copy(CustomerFilter);

    if Customer.FindSet() then
        repeat
            CustomerMutation.Init(Customer."No.");
            CustomerMutation.SetCity(NewCity);

            if CustomerMutation.Apply() then
                UpdateCount += 1
            else
                FailCount += 1;
        until Customer.Next() = 0;

    Message('Updated %1 customers. %2 failed.', UpdateCount, FailCount);
end;
```

### Batch Update with Different Values

```al
procedure ApplyBulkChanges(var TempCustomerChange: Record "Name/Value Buffer" temporary)
var
    CustomerMutation: Codeunit "SMC Customer Mutation";
    Customer: Record Customer;
begin
    // TempCustomerChange contains: Name = CustomerNo, Value = NewName
    if TempCustomerChange.FindSet() then
        repeat
            if Customer.Get(TempCustomerChange.Name) then begin
                CustomerMutation.Init(Customer."No.");
                CustomerMutation.SetName(CopyStr(TempCustomerChange.Value, 1, 100));
                CustomerMutation.Apply();
            end;
        until TempCustomerChange.Next() = 0;
end;
```

## Error Handling

### Try-Catch Pattern

```al
procedure SafeUpdateCustomer(CustomerNo: Code[20]; NewName: Text[100]): Boolean
var
    CustomerMutation: Codeunit "SMC Customer Mutation";
begin
    if not CustomerMutation.Init(CustomerNo) then
        exit(false);

    CustomerMutation.SetName(NewName);

    exit(CustomerMutation.Apply());
end;
```

### Detailed Error Handling

```al
procedure UpdateCustomerWithLogging(CustomerNo: Code[20]; NewName: Text[100]; NewCity: Text[30])
var
    CustomerMutation: Codeunit "SMC Customer Mutation";
    ErrorLogEntry: Record "Error Message";
begin
    CustomerMutation.Init(CustomerNo);
    CustomerMutation
        .SetName(NewName)
        .SetCity(NewCity);

    if not CustomerMutation.Apply() then begin
        // Log the error
        ErrorLogEntry.LogMessage(
            0,
            ErrorLogEntry."Message Type"::Error,
            DATABASE::Customer,
            0,
            StrSubstNo('Failed to update customer %1: %2', CustomerNo, GetLastErrorText())
        );

        Error('Customer update failed. See error log for details.');
    end;
end;
```

### Rollback-Safe Operations

```al
procedure UpdateCustomerTransactional(CustomerNo: Code[20]; NewName: Text[100])
var
    CustomerMutation: Codeunit "SMC Customer Mutation";
    OriginalName: Text[100];
    Customer: Record Customer;
begin
    Customer.Get(CustomerNo);
    OriginalName := Customer.Name;

    CustomerMutation.Init(CustomerNo);
    CustomerMutation.SetName(NewName);

    if not CustomerMutation.Apply() then begin
        // Restore original value if needed
        Customer.Get(CustomerNo);
        if Customer.Name <> OriginalName then begin
            CustomerMutation.Init(CustomerNo);
            CustomerMutation.SetName(OriginalName);
            CustomerMutation.Apply();
        end;
        Error('Failed to update customer');
    end;
end;
```

## Integration Patterns

### API Endpoint Handler

```al
procedure HandleCustomerUpdateAPI(CustomerNo: Code[20]; JsonPayload: JsonObject)
var
    CustomerMutation: Codeunit "SMC Customer Mutation";
    NameToken: JsonToken;
    AddressToken: JsonToken;
    CityToken: JsonToken;
begin
    CustomerMutation.Init(CustomerNo);

    if JsonPayload.Get('name', NameToken) then
        CustomerMutation.SetName(CopyStr(NameToken.AsValue().AsText(), 1, 100));

    if JsonPayload.Get('address', AddressToken) then
        CustomerMutation.SetAddress(CopyStr(AddressToken.AsValue().AsText(), 1, 100));

    if JsonPayload.Get('city', CityToken) then
        CustomerMutation.SetCity(CopyStr(CityToken.AsValue().AsText(), 1, 30));

    if not CustomerMutation.Apply() then
        Error('Failed to update customer from API request');
end;
```

### Page Action Integration

```al
page 50100 "Customer Mutation Example"
{
    PageType = Card;
    SourceTable = Customer;

    actions
    {
        area(Processing)
        {
            action(UpdateProfile)
            {
                Caption = 'Update Profile';
                Image = Customer;

                trigger OnAction()
                var
                    CustomerMutation: Codeunit "SMC Customer Mutation";
                begin
                    CustomerMutation.Init(Rec."No.");
                    CustomerMutation
                        .SetName('Updated via Action')
                        .SetCity('New York');

                    if CustomerMutation.Apply() then begin
                        Message('Customer updated successfully');
                        CurrPage.Update(false);
                    end else
                        Error('Failed to update customer');
                end;
            }
        }
    }
}
```

### Codeunit Event Subscriber

```al
codeunit 50103 "Customer Event Handler"
{
    [EventSubscriber(ObjectType::Table, Database::Customer, 'OnAfterInsertEvent', '', false, false)]
    local procedure OnAfterCustomerInsert(var Rec: Record Customer)
    var
        CustomerMutation: Codeunit "SMC Customer Mutation";
    begin
        // Set default values after customer creation
        CustomerMutation.Init(Rec."No.");
        CustomerMutation
            .When(Rec.City = '')
            .SetCity('Seattle');

        CustomerMutation.Apply();
    end;
}
```

### Import/Migration Scenario

```al
procedure ImportCustomersFromCSV(CSVBuffer: Record "CSV Buffer")
var
    CustomerMutation: Codeunit "SMC Customer Mutation";
    Customer: Record Customer;
    CustomerNo: Code[20];
    ImportedCount: Integer;
begin
    CSVBuffer.Reset();
    if CSVBuffer.FindSet() then
        repeat
            // Assuming CSV has: No, Name, Address, City columns
            if CSVBuffer."Field No." = 1 then
                CustomerNo := CopyStr(CSVBuffer.Value, 1, 20);

            // Create customer if doesn't exist
            if not Customer.Get(CustomerNo) then begin
                Customer.Init();
                Customer."No." := CustomerNo;
                Customer.Insert(true);
            end;

            // Use mutation to update fields
            CustomerMutation.Init(CustomerNo);

            case CSVBuffer."Field No." of
                2: CustomerMutation.SetName(CopyStr(CSVBuffer.Value, 1, 100));
                3: CustomerMutation.SetAddress(CopyStr(CSVBuffer.Value, 1, 100));
                4: CustomerMutation.SetCity(CopyStr(CSVBuffer.Value, 1, 30));
            end;

            if CSVBuffer."Field No." = 4 then begin // Last field, apply changes
                if CustomerMutation.Apply() then
                    ImportedCount += 1;
            end;
        until CSVBuffer.Next() = 0;

    Message('Imported/Updated %1 customers', ImportedCount);
end;
```

## Best Practices

1. **Always check Apply() result**: The `Apply()` method returns a boolean. Always check it in production code.

2. **Use type-safe methods when available**: Prefer `SetName()` over `ValidateField(2, Name)` for better compile-time safety.

3. **Chain operations for readability**: The fluent API is designed for chaining - use it to make your intent clear.

4. **Use When() for optional updates**: Instead of if-statements around each field, use `When()` for cleaner code.

5. **Validate when needed**: Only use `ValidateField()` when you need the OnValidate trigger to fire. Use `Set()` for better performance when validation isn't needed.

6. **Initialize once per record**: Call `Init()` once per record. Don't reuse the same mutation object for different records.

## Anti-Patterns to Avoid

### Don't: Ignore Apply() result
```al
// BAD
CustomerMutation.Init(CustomerNo);
CustomerMutation.SetName('New Name');
CustomerMutation.Apply(); // Result ignored!
```

### Don't: Reuse mutation object
```al
// BAD
CustomerMutation.Init('CUST001');
CustomerMutation.SetName('Name 1');
CustomerMutation.Apply();

CustomerMutation.SetName('Name 2'); // Still operating on CUST001!
CustomerMutation.Apply();
```

### Don't: Mix direct record modification with mutations
```al
// BAD - mixing paradigms
Customer.Get(CustomerNo);
Customer.Name := 'Direct Update';

CustomerMutation.Init(CustomerNo);
CustomerMutation.SetCity('Seattle');
CustomerMutation.Apply(); // May overwrite the direct Name change
```

## Performance Considerations

- Mutations use RecordRef internally, which has minimal overhead
- Each `Apply()` call results in one `Modify(true)` operation
- For bulk operations, consider batching and using standard BC patterns alongside mutations
- The `When()` clause adds negligible overhead - use it liberally for code clarity
