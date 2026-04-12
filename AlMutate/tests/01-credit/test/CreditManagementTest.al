codeunit 60010 "Credit Management Tests"
{
    Subtype = Test;

    var
        CreditMgmt: Codeunit "Credit Management";
        Assert: Codeunit Assert;

    // -----------------------------------------------------------------------
    // IsOverLimit tests — boundary at exactly the credit limit value
    // These tests KILL the rel-gt-to-gte mutation (> becomes >=):
    //   Original:  IsOverLimit(1000, 1000) = false  (1000 > 1000 is false)
    //   Mutated:   IsOverLimit(1000, 1000) = true   (1000 >= 1000 is true)  => KILLED
    // -----------------------------------------------------------------------

    [Test]
    procedure TestIsOverLimit_ExactLimit_ReturnsFalse()
    begin
        // [GIVEN] Amount equals the credit limit exactly
        // [WHEN] Checking over-limit
        // [THEN] Should NOT be over limit (boundary: > not >=)
        Assert.IsFalse(CreditMgmt.IsOverLimit(1000, 1000), 'Amount equal to limit must not be over limit');
    end;

    [Test]
    procedure TestIsOverLimit_AboveLimit_ReturnsTrue()
    begin
        // [GIVEN] Amount is one unit above the credit limit
        // [WHEN] Checking over-limit
        // [THEN] Should be over limit
        Assert.IsTrue(CreditMgmt.IsOverLimit(1001, 1000), 'Amount above limit must be over limit');
    end;

    [Test]
    procedure TestIsOverLimit_BelowLimit_ReturnsFalse()
    begin
        // [GIVEN] Amount is below the credit limit
        // [WHEN] Checking over-limit
        // [THEN] Should NOT be over limit
        Assert.IsFalse(CreditMgmt.IsOverLimit(500, 1000), 'Amount below limit must not be over limit');
    end;

    // -----------------------------------------------------------------------
    // ApplyDiscount tests — verifies arithmetic: BaseAmount - (BaseAmount * Pct / 100)
    // These tests KILL arith-mul-to-div and arith-div-to-mul mutations
    // -----------------------------------------------------------------------

    [Test]
    procedure TestApplyDiscount_10Percent()
    var
        Result: Decimal;
    begin
        // [GIVEN] Base amount 200, discount 10%
        // [WHEN] Applying discount
        // [THEN] Result should be 180
        Result := CreditMgmt.ApplyDiscount(200, 10);
        Assert.AreEqual(180, Result, 'Expected 10% discount on 200 to yield 180');
    end;

    [Test]
    procedure TestApplyDiscount_0Percent()
    var
        Result: Decimal;
    begin
        Result := CreditMgmt.ApplyDiscount(100, 0);
        Assert.AreEqual(100, Result, 'Zero discount should return original amount');
    end;

    [Test]
    procedure TestApplyDiscount_100Percent()
    var
        Result: Decimal;
    begin
        Result := CreditMgmt.ApplyDiscount(250, 100);
        Assert.AreEqual(0, Result, '100% discount should return zero');
    end;

    // -----------------------------------------------------------------------
    // GetFee tests — verifies boolean conditional: if Active then
    // -----------------------------------------------------------------------

    [Test]
    procedure TestGetFee_ActiveAccount_ReturnsFee()
    var
        Result: Decimal;
    begin
        Result := CreditMgmt.GetFee(true, 50);
        Assert.AreEqual(50, Result, 'Active account should return the fee amount');
    end;

    [Test]
    procedure TestGetFee_InactiveAccount_ReturnsZero()
    var
        Result: Decimal;
    begin
        Result := CreditMgmt.GetFee(false, 50);
        Assert.AreEqual(0, Result, 'Inactive account should return zero fee');
    end;
}
