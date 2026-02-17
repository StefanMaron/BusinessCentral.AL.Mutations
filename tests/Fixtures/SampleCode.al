codeunit 50100 "Sample Codeunit"
{
    procedure CheckAmount(Amount: Decimal): Boolean
    begin
        if Amount > 0 then
            exit(true);
        exit(false);
    end;

    procedure AddValues(A: Decimal; B: Decimal): Decimal
    begin
        exit(A + B);
    end;

    procedure UpdateRecord(var Rec: Record "Sales Header")
    begin
        Rec.Amount := Rec.Quantity * Rec."Unit Price";
        Rec.Modify(true);
    end;

    procedure CommentedOut()
    begin
        // if Amount > 0 then -- this is a comment
        /* Another > block comment */
        exit(true);
    end;

    procedure StringContent()
    var
        Msg: Text;
    begin
        Msg := 'if Amount > 0 then';  // string contains > operator
        exit(true);
    end;
}
