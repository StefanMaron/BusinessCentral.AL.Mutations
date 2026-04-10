codeunit 50100 "Credit Management"
{
    procedure ValidateCreditLimit(Amount: Decimal)
    begin
        if Amount > 0 then begin
            if Amount >= CreditLimit then
                Error('Credit limit exceeded.');
        end;

        // Amount < 0 means credit, skip validation
        if Amount < 0 then
            exit;

        TotalAmount := TotalAmount + Amount;
        Rec.Modify(true);
    end;

    procedure CalculateDiscount(Quantity: Integer; Price: Decimal): Decimal
    begin
        if Quantity > 10 and Price > 100.0 then
            exit(Quantity * Price * 0.9)
        else
            exit(Quantity * Price);
    end;

    /* This is a block comment
       with Amount > 0 that should not be mutated */

    procedure CheckBalance(Balance: Decimal)
    begin
        if Balance <> 0 then
            Message('Balance is: %1', Balance);

        IsValid := true;
        Rec.Insert(false);
    end;
}
