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

    procedure ProcessRecords()
    var
        Rec: Record Customer;
        Found: Boolean;
    begin
        // boolean literals
        Found := false;

        // unary not
        if not Rec.IsEmpty() then begin
            // exit with boolean
            if Found then
                exit(true);

            // TestField, SetRange, SetFilter
            Rec.TestField("No.");
            Rec.SetRange("Customer Group", 'A');
            Rec.SetFilter(Name, '%1', '*test*');

            // Init, Commit, DeleteAll
            Rec.Init();
            Commit();
            Rec.DeleteAll(true);

            // FindSet
            if Rec.FindSet() then
                repeat
                until Rec.Next() = 0;
        end;

        exit(false);
    end;
}
