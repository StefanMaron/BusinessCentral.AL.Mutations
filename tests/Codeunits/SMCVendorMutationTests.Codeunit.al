/// <summary>
/// Tests for SMCVendorMutation codeunit.
/// </summary>
codeunit 50151 "SMC Vendor Mutation Tests"
{
    Subtype = Test;

    var
        Assert: Codeunit "Library Assert";

    [Test]
    procedure TestVendorMutation_SetName_ShouldUpdateRecord()
    var
        Vendor: Record Vendor;
        VendorMutation: Codeunit "SMC Vendor Mutation";
        VendorNo: Code[20];
    begin
        // Arrange
        VendorNo := CreateTestVendor();

        // Act
        VendorMutation.Init(VendorNo);
        VendorMutation.SetName('Updated Vendor Name');
        VendorMutation.Apply();

        // Assert
        Vendor.Get(VendorNo);
        Assert.AreEqual('Updated Vendor Name', Vendor.Name, 'Vendor name should be updated');

        // Cleanup
        Vendor.Delete();
    end;

    [Test]
    procedure TestVendorMutation_SetAddress_ShouldUpdateRecord()
    var
        Vendor: Record Vendor;
        VendorMutation: Codeunit "SMC Vendor Mutation";
        VendorNo: Code[20];
    begin
        // Arrange
        VendorNo := CreateTestVendor();

        // Act
        VendorMutation.Init(VendorNo);
        VendorMutation.SetAddress('456 Vendor Street');
        VendorMutation.Apply();

        // Assert
        Vendor.Get(VendorNo);
        Assert.AreEqual('456 Vendor Street', Vendor.Address, 'Vendor address should be updated');

        // Cleanup
        Vendor.Delete();
    end;

    [Test]
    procedure TestVendorMutation_SetCity_ShouldUpdateRecord()
    var
        Vendor: Record Vendor;
        VendorMutation: Codeunit "SMC Vendor Mutation";
        VendorNo: Code[20];
    begin
        // Arrange
        VendorNo := CreateTestVendor();

        // Act
        VendorMutation.Init(VendorNo);
        VendorMutation.SetCity('Portland');
        VendorMutation.Apply();

        // Assert
        Vendor.Get(VendorNo);
        Assert.AreEqual('Portland', Vendor.City, 'Vendor city should be updated');

        // Cleanup
        Vendor.Delete();
    end;

    [Test]
    procedure TestVendorMutation_SetMultipleFields_ShouldUpdateAllFields()
    var
        Vendor: Record Vendor;
        VendorMutation: Codeunit "SMC Vendor Mutation";
        VendorNo: Code[20];
    begin
        // Arrange
        VendorNo := CreateTestVendor();

        // Act
        VendorMutation.Init(VendorNo);
        VendorMutation.SetName('Chain Vendor');
        VendorMutation.SetAddress('789 Chain Ave');
        VendorMutation.SetCity('Chicago');
        VendorMutation.Apply();

        // Assert
        Vendor.Get(VendorNo);
        Assert.AreEqual('Chain Vendor', Vendor.Name, 'Vendor name should be updated');
        Assert.AreEqual('789 Chain Ave', Vendor.Address, 'Vendor address should be updated');
        Assert.AreEqual('Chicago', Vendor.City, 'Vendor city should be updated');

        // Cleanup
        Vendor.Delete();
    end;

    [Test]
    procedure TestVendorMutation_FluentChaining_ShouldUpdateAllFields()
    var
        Vendor: Record Vendor;
        VendorMutation: Codeunit "SMC Vendor Mutation";
        VendorNo: Code[20];
    begin
        // Arrange
        VendorNo := CreateTestVendor();

        // Act - fluent chaining
        VendorMutation.Init(VendorNo);
        VendorMutation.SetName('Fluent Vendor').SetAddress('100 Fluent Blvd').SetCity('Denver').Apply();

        // Assert
        Vendor.Get(VendorNo);
        Assert.AreEqual('Fluent Vendor', Vendor.Name, 'Vendor name should be updated via chain');
        Assert.AreEqual('100 Fluent Blvd', Vendor.Address, 'Vendor address should be updated via chain');
        Assert.AreEqual('Denver', Vendor.City, 'Vendor city should be updated via chain');

        // Cleanup
        Vendor.Delete();
    end;

    [Test]
    procedure TestVendorMutation_WhenTrue_ShouldApplyChanges()
    var
        Vendor: Record Vendor;
        VendorMutation: Codeunit "SMC Vendor Mutation";
        VendorNo: Code[20];
    begin
        // Arrange
        VendorNo := CreateTestVendor();

        // Act
        VendorMutation.Init(VendorNo);
        VendorMutation.When(true);
        VendorMutation.SetName('Conditional Vendor');
        VendorMutation.Apply();

        // Assert
        Vendor.Get(VendorNo);
        Assert.AreEqual('Conditional Vendor', Vendor.Name, 'Name should be updated when condition is true');

        // Cleanup
        Vendor.Delete();
    end;

    [Test]
    procedure TestVendorMutation_WhenFalse_ShouldNotApplyChanges()
    var
        Vendor: Record Vendor;
        VendorMutation: Codeunit "SMC Vendor Mutation";
        VendorNo: Code[20];
    begin
        // Arrange
        VendorNo := CreateTestVendor();

        // Act
        VendorMutation.Init(VendorNo);
        VendorMutation.When(false);
        VendorMutation.SetName('Should Not Apply');
        VendorMutation.Apply();

        // Assert
        Vendor.Get(VendorNo);
        Assert.AreEqual('Test Vendor', Vendor.Name, 'Name should NOT be updated when condition is false');

        // Cleanup
        Vendor.Delete();
    end;

    [Test]
    procedure TestVendorMutation_ApplyWithoutChanges_ShouldSucceed()
    var
        Vendor: Record Vendor;
        VendorMutation: Codeunit "SMC Vendor Mutation";
        VendorNo: Code[20];
        OriginalName: Text[100];
        ApplyResult: Boolean;
    begin
        // Arrange
        VendorNo := CreateTestVendor();
        Vendor.Get(VendorNo);
        OriginalName := Vendor.Name;

        // Act
        VendorMutation.Init(VendorNo);
        ApplyResult := VendorMutation.Apply();

        // Assert
        Assert.IsTrue(ApplyResult, 'Apply should succeed even with no changes');
        Vendor.Get(VendorNo);
        Assert.AreEqual(OriginalName, Vendor.Name, 'Vendor name should remain unchanged');

        // Cleanup
        Vendor.Delete();
    end;

    [Test]
    procedure TestVendorMutation_InitWithInvalidVendor_ShouldThrowError()
    var
        VendorMutation: Codeunit "SMC Vendor Mutation";
    begin
        // Act & Assert
        asserterror VendorMutation.Init('NONEXISTENT99');
    end;

    [Test]
    procedure TestVendorMutation_ApplyWithoutInit_ShouldReturnFalse()
    var
        VendorMutation: Codeunit "SMC Vendor Mutation";
        ApplyResult: Boolean;
    begin
        // Act
        ApplyResult := VendorMutation.Apply();

        // Assert
        Assert.IsFalse(ApplyResult, 'Apply should return false when not initialized');
    end;

    [Test]
    procedure TestVendorMutation_ValidateField_ShouldTriggerValidation()
    var
        Vendor: Record Vendor;
        VendorMutation: Codeunit "SMC Vendor Mutation";
        VendorNo: Code[20];
    begin
        // Arrange
        VendorNo := CreateTestVendor();

        // Act - validate Name field
        VendorMutation.Init(VendorNo);
        VendorMutation.ValidateField(Vendor.FieldNo(Name), 'Validated Vendor Name');
        VendorMutation.Apply();

        // Assert
        Vendor.Get(VendorNo);
        Assert.AreEqual('Validated Vendor Name', Vendor.Name, 'Vendor name should be updated via ValidateField');

        // Cleanup
        Vendor.Delete();
    end;

    local procedure CreateTestVendor(): Code[20]
    var
        Vendor: Record Vendor;
    begin
        Vendor.Init();
        Vendor."No." := 'VTST-' + Format(Random(99999));
        Vendor.Name := 'Test Vendor';
        Vendor.Insert(true);
        exit(Vendor."No.");
    end;
}
