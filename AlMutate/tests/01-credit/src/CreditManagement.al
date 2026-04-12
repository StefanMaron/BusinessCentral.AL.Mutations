codeunit 60000 "Credit Management"
{
    /// <summary>
    /// Returns true when the amount exceeds the credit limit.
    /// Mutation target: '>' on line with Amount > CreditLimit
    /// </summary>
    procedure IsOverLimit(Amount: Decimal; CreditLimit: Decimal): Boolean
    begin
        if Amount > CreditLimit then
            exit(true);
        exit(false);
    end;

    /// <summary>
    /// Applies a percentage discount to a base amount.
    /// Mutation target: '*' and '/' arithmetic operators
    /// </summary>
    procedure ApplyDiscount(BaseAmount: Decimal; DiscountPct: Decimal): Decimal
    begin
        exit(BaseAmount - (BaseAmount * DiscountPct / 100));
    end;

    /// <summary>
    /// Returns the fee only when the account is active.
    /// Mutation target: boolean 'true' literal
    /// </summary>
    procedure GetFee(Active: Boolean; FeeAmount: Decimal): Decimal
    begin
        if Active then
            exit(FeeAmount);
        exit(0);
    end;
}
