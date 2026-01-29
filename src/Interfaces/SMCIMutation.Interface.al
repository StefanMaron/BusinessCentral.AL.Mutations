/// <summary>
/// Core interface for mutation operations.
/// Provides a fluent API for modifying Business Central records.
/// </summary>
interface "SMC IMutation"
{
    /// <summary>
    /// Sets a field value without validation.
    /// </summary>
    /// <param name="FieldNo">The field number to set.</param>
    /// <param name="Value">The value to assign.</param>
    procedure Set(FieldNo: Integer; Value: Variant);

    /// <summary>
    /// Sets a field value with validation.
    /// </summary>
    /// <param name="FieldNo">The field number to validate.</param>
    /// <param name="Value">The value to validate and assign.</param>
    procedure Validate(FieldNo: Integer; Value: Variant);

    /// <summary>
    /// Conditionally applies subsequent operations.
    /// </summary>
    /// <param name="Condition">If true, subsequent operations are applied; otherwise, they are skipped.</param>
    procedure When(Condition: Boolean);

    /// <summary>
    /// Applies all pending mutations to the record.
    /// </summary>
    /// <returns>True if the operation succeeded; otherwise, false.</returns>
    procedure Apply(): Boolean;
}
