/// <summary>
/// Base implementation of mutation operations.
/// Provides common functionality for all entity-specific mutations.
/// </summary>
codeunit 50100 "SMC Mutation Base" implements "SMC IMutation"
{
    var
        MutationRecRef: RecordRef;
        IsInitialized: Boolean;
        ConditionActive: Boolean;

    /// <summary>
    /// Initializes the mutation with a record reference.
    /// </summary>
    /// <param name="RecRef">The record reference to mutate.</param>
    procedure Initialize(RecRef: RecordRef)
    begin
        MutationRecRef := RecRef;
        IsInitialized := true;
        ConditionActive := true;
    end;

    /// <summary>
    /// Sets a field value without validation.
    /// </summary>
    /// <param name="FieldNo">The field number to set.</param>
    /// <param name="Value">The value to assign.</param>
    procedure Set(FieldNo: Integer; Value: Variant)
    var
        FieldRef: FieldRef;
    begin
        if not ShouldApplyOperation() then
            exit;

        FieldRef := MutationRecRef.Field(FieldNo);
        FieldRef.Value := Value;
    end;

    /// <summary>
    /// Sets a field value with validation.
    /// </summary>
    /// <param name="FieldNo">The field number to validate.</param>
    /// <param name="Value">The value to validate and assign.</param>
    procedure Validate(FieldNo: Integer; Value: Variant)
    var
        FieldRef: FieldRef;
    begin
        if not ShouldApplyOperation() then
            exit;

        FieldRef := MutationRecRef.Field(FieldNo);
        FieldRef.Validate(Value);
    end;

    /// <summary>
    /// Conditionally applies subsequent operations.
    /// </summary>
    /// <param name="Condition">If true, subsequent operations are applied; otherwise, they are skipped.</param>
    procedure When(Condition: Boolean)
    begin
        ConditionActive := Condition;
    end;

    /// <summary>
    /// Applies all pending mutations to the record.
    /// </summary>
    /// <returns>True if the operation succeeded; otherwise, false.</returns>
    procedure Apply(): Boolean
    begin
        if not IsInitialized then
            exit(false);

        exit(MutationRecRef.Modify(true));
    end;

    local procedure ShouldApplyOperation(): Boolean
    begin
        exit(IsInitialized and ConditionActive);
    end;

    /// <summary>
    /// Gets the underlying record reference for testing or advanced scenarios.
    /// </summary>
    /// <returns>The record reference being mutated.</returns>
    procedure GetRecordRef(): RecordRef
    begin
        exit(MutationRecRef);
    end;
}
