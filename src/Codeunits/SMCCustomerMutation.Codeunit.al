/// <summary>
/// Customer-specific mutation implementation.
/// Provides fluent API for Customer record operations.
/// </summary>
codeunit 50101 "SMC Customer Mutation"
{
    var
        MutationBase: Codeunit "SMC Mutation Base";
        CustomerRec: Record Customer;

    /// <summary>
    /// Initializes the mutation for a specific customer.
    /// </summary>
    /// <param name="CustomerNo">The customer number to mutate.</param>
    procedure Init(CustomerNo: Code[20])
    var
        RecRef: RecordRef;
    begin
        CustomerRec.Get(CustomerNo);
        RecRef.GetTable(CustomerRec);
        MutationBase.Initialize(RecRef);
    end;

    /// <summary>
    /// Sets the customer name.
    /// </summary>
    /// <param name="Name">The name to set.</param>
    /// <returns>The mutation instance for chaining.</returns>
    procedure SetName(Name: Text[100]): Codeunit "SMC Customer Mutation"
    begin
        MutationBase.Set(CustomerRec.FieldNo(Name), Name);
        exit(this);
    end;

    /// <summary>
    /// Sets the customer address.
    /// </summary>
    /// <param name="Address">The address to set.</param>
    /// <returns>The mutation instance for chaining.</returns>
    procedure SetAddress(Address: Text[100]): Codeunit "SMC Customer Mutation"
    begin
        MutationBase.Set(CustomerRec.FieldNo(Address), Address);
        exit(this);
    end;

    /// <summary>
    /// Sets the customer city.
    /// </summary>
    /// <param name="City">The city to set.</param>
    /// <returns>The mutation instance for chaining.</returns>
    procedure SetCity(City: Text[30]): Codeunit "SMC Customer Mutation"
    begin
        MutationBase.Set(CustomerRec.FieldNo(City), City);
        exit(this);
    end;

    /// <summary>
    /// Validates and sets a field by field number.
    /// </summary>
    /// <param name="FieldNo">The field number to validate.</param>
    /// <param name="Value">The value to validate and set.</param>
    /// <returns>The mutation instance for chaining.</returns>
    procedure ValidateField(FieldNo: Integer; Value: Variant): Codeunit "SMC Customer Mutation"
    begin
        MutationBase.Validate(FieldNo, Value);
        exit(this);
    end;

    /// <summary>
    /// Conditionally applies subsequent operations.
    /// </summary>
    /// <param name="Condition">If true, subsequent operations are applied; otherwise, they are skipped.</param>
    /// <returns>The mutation instance for chaining.</returns>
    procedure When(Condition: Boolean): Codeunit "SMC Customer Mutation"
    begin
        MutationBase.When(Condition);
        exit(this);
    end;

    /// <summary>
    /// Applies all pending mutations to the customer record.
    /// </summary>
    /// <returns>True if the operation succeeded; otherwise, false.</returns>
    procedure Apply(): Boolean
    var
        RecRef: RecordRef;
    begin
        RecRef := MutationBase.GetRecordRef();
        RecRef.SetTable(CustomerRec);
        exit(MutationBase.Apply());
    end;
}
