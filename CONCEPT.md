# BusinessCentral.AL.Mutations - Product Concept

## Vision

A type-safe, chainable mutation helper for Microsoft Dynamics 365 Business Central,
providing a Prisma-like developer experience for BC API operations in AL code.

## Problem Statement

Business Central's record modification patterns are verbose and error-prone:

- Manual field-by-field assignments
- No fluent/chainable API
- Repetitive boilerplate for common patterns
- Easy to forget Modify() calls
- No built-in validation helpers

## Solution

```al
// Instead of this:
Customer.Get(CustomerNo);
Customer.Name := 'New Name';
Customer.Address := '123 Main St';
Customer.City := 'Seattle';
Customer.Validate("Payment Terms Code", '30 DAYS');
Customer.Modify(true);

// Write this:
Mutate.Customer(CustomerNo)
    .Set(Name, 'New Name')
    .Set(Address, '123 Main St')
    .Set(City, 'Seattle')
    .Validate("Payment Terms Code", '30 DAYS')
    .Apply();
```

## Core Features

1. **Fluent API** - Chainable method calls for clean, readable code
2. **Type Safety** - Compile-time field validation
3. **Auto-Modify** - Handles Get/Modify lifecycle automatically
4. **Batch Operations** - Efficient bulk mutations
5. **Validation Helpers** - Built-in Validate() support
6. **Conditional Mutations** - When() clauses for conditional field updates
7. **Audit Trail** - Optional change tracking

## Technical Approach

- AL codeunit-based implementation
- Interface pattern for extensibility
- Generic record handling where possible
- Comprehensive test coverage with AL Test Framework

## Target Tables (Initial Scope)

1. Customer
2. Vendor
3. Item
4. Sales Header / Sales Line
5. Purchase Header / Purchase Line
6. General Journal Line
7. Item Journal Line

## Success Criteria

- [ ] Fluent API for top 10 BC entities
- [ ] 100% test coverage
- [ ] Clear documentation with examples
- [ ] Works with BC v22+ (current + 2 previous versions)
- [ ] Published to GitHub with MIT license

## Non-Goals (v1)

- External API support (OData/SOAP) - focus on AL-internal mutations
- UI components
- Permission set management
