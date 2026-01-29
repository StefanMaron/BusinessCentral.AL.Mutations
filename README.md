# BusinessCentral.AL.Mutations

A type-safe, chainable mutation helper for Microsoft Dynamics 365 Business Central.

> **Note**: This project is developed autonomously by Claude. Create an issue to request features or report bugs!

## Vision

Replace verbose field-by-field record modifications with a clean, fluent API:

```al
// Before: Verbose and error-prone
Customer.Get(CustomerNo);
Customer.Name := 'New Name';
Customer.Address := '123 Main St';
Customer.City := 'Seattle';
Customer.Validate("Payment Terms Code", '30 DAYS');
Customer.Modify(true);

// After: Clean and chainable
Mutate.Customer(CustomerNo)
    .Set(Name, 'New Name')
    .Set(Address, '123 Main St')
    .Set(City, 'Seattle')
    .Validate("Payment Terms Code", '30 DAYS')
    .Apply();
```

## Features

- **Fluent API** - Chainable method calls
- **Type Safety** - Compile-time validation
- **Auto-Modify** - Handles Get/Modify lifecycle
- **Batch Operations** - Efficient bulk mutations
- **Validation Support** - Built-in Validate() handling

## Installation

*Coming soon - will be available via AL package*

## Usage

*Documentation will be added as features are implemented*

## Contributing

This project uses an AI-first development model:

1. **Create an Issue** - Describe what you want
2. **Claude Implements** - The AI developer picks it up
3. **Review PR** - Human reviews and approves
4. **Merged!** - Feature ships

### Commands in Issues

- `@claude` or `/claude` - Get Claude's attention
- `/implement` - Ask Claude to start implementing
- `/status` - Get progress update
- `/plan` - See implementation plan

## License

MIT License - See [LICENSE](LICENSE)

## Author

Created by Stefan Maron, developed by Claude.
