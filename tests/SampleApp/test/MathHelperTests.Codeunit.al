codeunit 70001 "Math Helper Tests"
{
    Subtype = Test;
    TestPermissions = Disabled;

    var
        Assert: Codeunit Assert;
        MathHelper: Codeunit "Math Helper";

    [Test]
    procedure TestIsPositive_PositiveValue_ReturnsTrue()
    begin
        // [SCENARIO] IsPositive returns true for positive values

        // [GIVEN] A positive decimal value
        // [WHEN] IsPositive is called
        // [THEN] It returns true
        Assert.IsTrue(MathHelper.IsPositive(100), 'IsPositive should return true for 100');
        Assert.IsTrue(MathHelper.IsPositive(0.01), 'IsPositive should return true for 0.01');
    end;

    [Test]
    procedure TestIsPositive_ZeroAndNegative_ReturnsFalse()
    begin
        // [SCENARIO] IsPositive returns false for zero and negative values

        // [GIVEN] Zero or negative decimal values
        // [WHEN] IsPositive is called
        // [THEN] It returns false
        Assert.IsFalse(MathHelper.IsPositive(0), 'IsPositive should return false for 0');
        Assert.IsFalse(MathHelper.IsPositive(-50), 'IsPositive should return false for -50');
    end;

    [Test]
    procedure TestIsInRange_ValueWithinRange_ReturnsTrue()
    begin
        // [SCENARIO] IsInRange returns true when value is within bounds

        // [GIVEN] A value within the specified range
        // [WHEN] IsInRange is called
        // [THEN] It returns true
        Assert.IsTrue(MathHelper.IsInRange(50, 0, 100), 'IsInRange should return true for 50 in [0, 100]');
        Assert.IsTrue(MathHelper.IsInRange(0, 0, 100), 'IsInRange should return true for 0 in [0, 100]');
        Assert.IsTrue(MathHelper.IsInRange(100, 0, 100), 'IsInRange should return true for 100 in [0, 100]');
    end;

    [Test]
    procedure TestIsInRange_ValueOutsideRange_ReturnsFalse()
    begin
        // [SCENARIO] IsInRange returns false when value is outside bounds

        // [GIVEN] A value outside the specified range
        // [WHEN] IsInRange is called
        // [THEN] It returns false
        Assert.IsFalse(MathHelper.IsInRange(-1, 0, 100), 'IsInRange should return false for -1 in [0, 100]');
        Assert.IsFalse(MathHelper.IsInRange(101, 0, 100), 'IsInRange should return false for 101 in [0, 100]');
    end;

    [Test]
    procedure TestAddValues()
    begin
        // [SCENARIO] AddValues correctly adds two decimal values

        // [GIVEN] Two decimal values
        // [WHEN] AddValues is called
        // [THEN] It returns their sum
        Assert.AreEqual(15.0, MathHelper.AddValues(10, 5), 'AddValues(10, 5) should return 15');
        Assert.AreEqual(0.0, MathHelper.AddValues(5, -5), 'AddValues(5, -5) should return 0');
        Assert.AreEqual(-10.0, MathHelper.AddValues(-3, -7), 'AddValues(-3, -7) should return -10');
    end;

    [Test]
    procedure TestMultiplyValues()
    begin
        // [SCENARIO] MultiplyValues correctly multiplies two decimal values

        // [GIVEN] Two decimal values
        // [WHEN] MultiplyValues is called
        // [THEN] It returns their product
        Assert.AreEqual(50.0, MathHelper.MultiplyValues(10, 5), 'MultiplyValues(10, 5) should return 50');
        Assert.AreEqual(0.0, MathHelper.MultiplyValues(0, 100), 'MultiplyValues(0, 100) should return 0');
        Assert.AreEqual(-20.0, MathHelper.MultiplyValues(-4, 5), 'MultiplyValues(-4, 5) should return -20');
    end;

    [Test]
    procedure TestCalculateDiscount()
    begin
        // [SCENARIO] CalculateDiscount correctly calculates discounted prices

        // [GIVEN] An original price and discount percentage
        // [WHEN] CalculateDiscount is called
        // [THEN] It returns the correct discounted price
        Assert.AreEqual(80.0, MathHelper.CalculateDiscount(100, 20), 'CalculateDiscount(100, 20) should return 80');
        Assert.AreEqual(100.0, MathHelper.CalculateDiscount(100, 0), 'CalculateDiscount(100, 0) should return 100');
        Assert.AreEqual(0.0, MathHelper.CalculateDiscount(100, 100), 'CalculateDiscount(100, 100) should return 0');
    end;

    [Test]
    procedure TestCalculateDiscount_InvalidPercentages()
    begin
        // [SCENARIO] CalculateDiscount handles invalid discount percentages

        // [GIVEN] Invalid discount percentages (negative or > 100)
        // [WHEN] CalculateDiscount is called
        // [THEN] It clamps the percentage to valid range
        Assert.AreEqual(100.0, MathHelper.CalculateDiscount(100, -10), 'Negative discount should be clamped to 0');
        Assert.AreEqual(0.0, MathHelper.CalculateDiscount(100, 150), 'Discount > 100 should be clamped to 100');
    end;

    [Test]
    procedure TestBothTrue()
    begin
        // [SCENARIO] BothTrue correctly evaluates AND logic

        // [GIVEN] Two boolean conditions
        // [WHEN] BothTrue is called
        // [THEN] It returns true only when both are true
        Assert.IsTrue(MathHelper.BothTrue(true, true), 'BothTrue(true, true) should return true');
        Assert.IsFalse(MathHelper.BothTrue(true, false), 'BothTrue(true, false) should return false');
        Assert.IsFalse(MathHelper.BothTrue(false, true), 'BothTrue(false, true) should return false');
        Assert.IsFalse(MathHelper.BothTrue(false, false), 'BothTrue(false, false) should return false');
    end;

    [Test]
    procedure TestEitherTrue()
    begin
        // [SCENARIO] EitherTrue correctly evaluates OR logic

        // [GIVEN] Two boolean conditions
        // [WHEN] EitherTrue is called
        // [THEN] It returns true when at least one is true
        Assert.IsTrue(MathHelper.EitherTrue(true, true), 'EitherTrue(true, true) should return true');
        Assert.IsTrue(MathHelper.EitherTrue(true, false), 'EitherTrue(true, false) should return true');
        Assert.IsTrue(MathHelper.EitherTrue(false, true), 'EitherTrue(false, true) should return true');
        Assert.IsFalse(MathHelper.EitherTrue(false, false), 'EitherTrue(false, false) should return false');
    end;
}
