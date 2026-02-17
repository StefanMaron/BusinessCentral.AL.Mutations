codeunit 70000 "Math Helper"
{
    /// <summary>
    /// Checks if a decimal value is positive.
    /// </summary>
    /// <param name="Value">The value to check.</param>
    /// <returns>True if positive, false otherwise.</returns>
    procedure IsPositive(Value: Decimal): Boolean
    begin
        if Value > 0 then
            exit(true);
        exit(false);
    end;

    /// <summary>
    /// Checks if a decimal value is within a range.
    /// </summary>
    /// <param name="Value">The value to check.</param>
    /// <param name="MinValue">Minimum of the range.</param>
    /// <param name="MaxValue">Maximum of the range.</param>
    /// <returns>True if within range, false otherwise.</returns>
    procedure IsInRange(Value: Decimal; MinValue: Decimal; MaxValue: Decimal): Boolean
    begin
        if (Value >= MinValue) and (Value <= MaxValue) then
            exit(true);
        exit(false);
    end;

    /// <summary>
    /// Adds two decimal values.
    /// </summary>
    /// <param name="A">First value.</param>
    /// <param name="B">Second value.</param>
    /// <returns>Sum of A and B.</returns>
    procedure AddValues(A: Decimal; B: Decimal): Decimal
    begin
        exit(A + B);
    end;

    /// <summary>
    /// Multiplies two decimal values.
    /// </summary>
    /// <param name="A">First value.</param>
    /// <param name="B">Second value.</param>
    /// <returns>Product of A and B.</returns>
    procedure MultiplyValues(A: Decimal; B: Decimal): Decimal
    begin
        exit(A * B);
    end;

    /// <summary>
    /// Calculates a discounted price.
    /// </summary>
    /// <param name="OriginalPrice">The original price.</param>
    /// <param name="DiscountPercent">Discount percentage (0-100).</param>
    /// <returns>The discounted price.</returns>
    procedure CalculateDiscount(OriginalPrice: Decimal; DiscountPercent: Decimal): Decimal
    var
        Discount: Decimal;
    begin
        if DiscountPercent < 0 then
            DiscountPercent := 0;
        if DiscountPercent > 100 then
            DiscountPercent := 100;

        Discount := OriginalPrice * DiscountPercent / 100;
        exit(OriginalPrice - Discount);
    end;

    /// <summary>
    /// Checks if both conditions are true.
    /// </summary>
    /// <param name="Condition1">First condition.</param>
    /// <param name="Condition2">Second condition.</param>
    /// <returns>True if both are true.</returns>
    procedure BothTrue(Condition1: Boolean; Condition2: Boolean): Boolean
    begin
        if Condition1 and Condition2 then
            exit(true);
        exit(false);
    end;

    /// <summary>
    /// Checks if either condition is true.
    /// </summary>
    /// <param name="Condition1">First condition.</param>
    /// <param name="Condition2">Second condition.</param>
    /// <returns>True if either is true.</returns>
    procedure EitherTrue(Condition1: Boolean; Condition2: Boolean): Boolean
    begin
        if Condition1 or Condition2 then
            exit(true);
        exit(false);
    end;
}
