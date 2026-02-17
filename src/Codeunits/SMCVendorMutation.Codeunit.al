/// <summary>
/// Vendor-specific mutation implementation.
/// Provides fluent API for Vendor record operations.
/// </summary>
codeunit 50102 "SMC Vendor Mutation"
{
    var
        MutationBase: Codeunit "SMC Mutation Base";

    /// <summary>
    /// Initializes the mutation for a specific vendor.
    /// </summary>
    /// <param name="VendorNo">The vendor number to mutate.</param>
    procedure Init(VendorNo: Code[20])
    var
        Vendor: Record Vendor;
        RecRef: RecordRef;
    begin
        Vendor.Get(VendorNo);
        RecRef.GetTable(Vendor);
        MutationBase.Initialize(RecRef);
    end;

    /// <summary>
    /// Sets the vendor name.
    /// </summary>
    /// <param name="Name">The name to set.</param>
    /// <returns>The mutation instance for chaining.</returns>
    procedure SetName(Name: Text[100]): Codeunit "SMC Vendor Mutation"
    var
        Vendor: Record Vendor;
    begin
        MutationBase.Set(Vendor.FieldNo(Name), Name);
        exit(this);
    end;

    /// <summary>
    /// Sets the vendor address.
    /// </summary>
    /// <param name="Address">The address to set.</param>
    /// <returns>The mutation instance for chaining.</returns>
    procedure SetAddress(Address: Text[100]): Codeunit "SMC Vendor Mutation"
    var
        Vendor: Record Vendor;
    begin
        MutationBase.Set(Vendor.FieldNo(Address), Address);
        exit(this);
    end;

    /// <summary>
    /// Sets the vendor city.
    /// </summary>
    /// <param name="City">The city to set.</param>
    /// <returns>The mutation instance for chaining.</returns>
    procedure SetCity(City: Text[30]): Codeunit "SMC Vendor Mutation"
    var
        Vendor: Record Vendor;
    begin
        MutationBase.Set(Vendor.FieldNo(City), City);
        exit(this);
    end;

    /// <summary>
    /// Validates and sets a field by field number.
    /// </summary>
    /// <param name="FieldNo">The field number to validate.</param>
    /// <param name="Value">The value to validate and set.</param>
    /// <returns>The mutation instance for chaining.</returns>
    procedure ValidateField(FieldNo: Integer; Value: Variant): Codeunit "SMC Vendor Mutation"
    begin
        MutationBase.Validate(FieldNo, Value);
        exit(this);
    end;

    /// <summary>
    /// Conditionally applies subsequent operations.
    /// </summary>
    /// <param name="Condition">If true, subsequent operations are applied; otherwise, they are skipped.</param>
    /// <returns>The mutation instance for chaining.</returns>
    procedure When(Condition: Boolean): Codeunit "SMC Vendor Mutation"
    begin
        MutationBase.When(Condition);
        exit(this);
    end;

    /// <summary>
    /// Applies all pending mutations to the vendor record.
    /// </summary>
    /// <returns>True if the operation succeeded; otherwise, false.</returns>
    procedure Apply(): Boolean
    begin
        exit(MutationBase.Apply());
    end;
}
