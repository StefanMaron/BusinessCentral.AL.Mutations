codeunit 50200 "Edge Cases"
{
    // Fixture for scanner edge case tests.

    // --- string concatenation ---
    // arith-add-to-sub must NOT produce a candidate here because '-' is
    // invalid for Text operands in AL.
    procedure StringConcat(Name: Text; Suffix: Text): Text
    begin
        exit(Name + ' ' + Suffix);
    end;

    // --- [EventSubscriber] attribute with boolean arguments ---
    // bool-true-to-false and bool-false-to-true must NOT target the 'true'/'false'
    // inside the attribute argument list — mutating them causes compile errors.
    [EventSubscriber(ObjectType::Codeunit, Codeunit::"Base App", 'OnAfterRun', '', true, false)]
    local procedure OnAfterRunSubscriber()
    begin
    end;

    // --- duplicate-producing operators ---
    // Insert(true) is targeted by both 'bc-insert-trigger-true' (changes true→false via
    // argument match) and 'bool-true-to-false' (changes true→false via literal).
    // Only ONE candidate should be emitted because both produce identical mutations.
    procedure DoInsert()
    begin
        Rec.Insert(true);
    end;
}
