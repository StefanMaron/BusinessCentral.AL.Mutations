codeunit 50100 "Credit Management"
{
    // Line 3: comment with > that must NOT be mutated
    // Amount > 0 should be ignored here

    procedure ValidateCreditLimit(Amount: Decimal)
    begin
        // Line 8: relational > (rel-gt-to-gte)
        if Amount > 0 then begin
            // Line 10: relational >= (rel-gte-to-gt)
            if Amount >= 1000 then
                // Line 12: statement-removal - Error (stmt-remove-error)
                Error('Credit limit exceeded.');
        end;

        // Line 16: relational < (rel-lt-to-lte)
        if Amount < 0 then
            exit;

        // Line 20: arithmetic + (arith-add-to-sub)
        Amount := Amount + 100;

        // Line 23: relational <= (rel-lte-to-lt)
        if Amount <= 500 then begin
            // Line 25: arithmetic - (arith-sub-to-add)
            Amount := Amount - 50;
        end;

        // Line 29: relational = (rel-eq-to-neq)
        if Amount = 0 then
            exit;

        // Line 33: statement-removal - Modify (stmt-remove-modify), bc-specific (bc-modify-trigger-true)
        Rec.Modify(true);
    end;

    procedure CalculateDiscount(Quantity: Integer; Price: Decimal): Decimal
    begin
        // Line 39: logical and (logic-and-to-or)
        if Quantity > 10 and Price > 100.0 then
            // Line 41: arithmetic * (arith-mul-to-div)
            exit(Quantity * Price * 0.9)
        else
            // Line 44: arithmetic * again
            exit(Quantity * Price);
    end;

    /* Block comment: Amount > 0 inside block comment - NOT mutated */

    procedure CheckBalance(Balance: Decimal)
    begin
        // Line 51: relational <> (rel-neq-to-eq)
        if Balance <> 0 then
            Message('Balance is: %1', Balance);

        // Line 55: boolean true (bool-true-to-false)
        IsValid := true;

        // Line 58: statement-removal - Insert (stmt-remove-insert), bc-specific
        Rec.Insert(false);
    end;

    procedure ProcessRecords()
    var
        Rec: Record Customer;
        Found: Boolean;
    begin
        // Line 65: boolean false (bool-false-to-true)
        Found := false;

        // Line 68: unary not (unary-remove-not)
        if not Rec.IsEmpty() then begin
            // Line 70: exit with true (exit-true-to-false)
            if Found then
                exit(true);

            // Line 74: statement-removal - TestField (stmt-remove-testfield)
            Rec.TestField("No.");
            // Line 76: statement-removal - SetRange (stmt-remove-setrange)
            Rec.SetRange("Customer Group", 'A');
            // Line 78: statement-removal - SetFilter (stmt-remove-setfilter)
            Rec.SetFilter(Name, '%1', '*test*');

            // Line 81: statement-removal - Init (stmt-remove-init)
            Rec.Init();
            // Line 83: statement-removal - Commit (stmt-remove-commit)
            Commit();
            // Line 85: statement-removal - DeleteAll, bc-specific (bc-deleteall-trigger-true)
            Rec.DeleteAll(true);

            // Line 88: bc-specific - FindSet to FindFirst (bc-findset-to-findfirst)
            if Rec.FindSet() then
                repeat
                until Rec.Next() = 0;
        end;

        // Line 93: logical or (logic-or-to-and)
        if Found or IsEmpty then
            Message('No records');

        // Line 97: arithmetic mod
        Amount := Amount mod 10;

        // Line 100: exit with false (exit-false-to-true)
        exit(false);
    end;
}
