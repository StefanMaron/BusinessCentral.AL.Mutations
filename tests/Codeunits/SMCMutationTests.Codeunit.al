/// <summary>
/// Tests for the SMC Mutation framework.
/// </summary>
codeunit 50150 "SMC Mutation Tests"
{
    Subtype = Test;

    var
        Assert: Codeunit "Library Assert";

    [Test]
    procedure TestCustomerMutation_SetName_ShouldUpdateRecord()
    var
        Customer: Record Customer;
        CustomerMutation: Codeunit "SMC Customer Mutation";
        CustomerNo: Code[20];
    begin
        // Arrange
        CustomerNo := CreateTestCustomer();

        // Act
        CustomerMutation.Init(CustomerNo);
        CustomerMutation.SetName('Test Customer Updated');
        CustomerMutation.Apply();

        // Assert
        Customer.Get(CustomerNo);
        Assert.AreEqual('Test Customer Updated', Customer.Name, 'Customer name should be updated');

        // Cleanup
        Customer.Delete();
    end;

    [Test]
    procedure TestCustomerMutation_SetMultipleFields_ShouldUpdateAllFields()
    var
        Customer: Record Customer;
        CustomerMutation: Codeunit "SMC Customer Mutation";
        CustomerNo: Code[20];
    begin
        // Arrange
        CustomerNo := CreateTestCustomer();

        // Act
        CustomerMutation.Init(CustomerNo);
        CustomerMutation.SetName('Updated Name');
        CustomerMutation.SetAddress('123 Main Street');
        CustomerMutation.SetCity('Seattle');
        CustomerMutation.Apply();

        // Assert
        Customer.Get(CustomerNo);
        Assert.AreEqual('Updated Name', Customer.Name, 'Customer name should be updated');
        Assert.AreEqual('123 Main Street', Customer.Address, 'Customer address should be updated');
        Assert.AreEqual('Seattle', Customer.City, 'Customer city should be updated');

        // Cleanup
        Customer.Delete();
    end;

    [Test]
    procedure TestCustomerMutation_ApplyWithoutChanges_ShouldNotFail()
    var
        Customer: Record Customer;
        CustomerMutation: Codeunit "SMC Customer Mutation";
        CustomerNo: Code[20];
        OriginalName: Text[100];
        ApplyResult: Boolean;
    begin
        // Arrange
        CustomerNo := CreateTestCustomer();
        Customer.Get(CustomerNo);
        OriginalName := Customer.Name;

        // Act
        CustomerMutation.Init(CustomerNo);
        ApplyResult := CustomerMutation.Apply();

        // Assert
        Assert.IsTrue(ApplyResult, 'Apply should succeed even with no changes');
        Customer.Get(CustomerNo);
        Assert.AreEqual(OriginalName, Customer.Name, 'Customer name should remain unchanged');

        // Cleanup
        Customer.Delete();
    end;

    [Test]
    procedure TestCustomerMutation_WhenTrue_ShouldApplyChanges()
    var
        Customer: Record Customer;
        CustomerMutation: Codeunit "SMC Customer Mutation";
        CustomerNo: Code[20];
    begin
        // Arrange
        CustomerNo := CreateTestCustomer();

        // Act
        CustomerMutation.Init(CustomerNo);
        CustomerMutation.When(true).SetName('When True Name');
        CustomerMutation.Apply();

        // Assert
        Customer.Get(CustomerNo);
        Assert.AreEqual('When True Name', Customer.Name, 'Name should be updated when condition is true');

        // Cleanup
        Customer.Delete();
    end;

    [Test]
    procedure TestCustomerMutation_WhenFalse_ShouldNotApplyChanges()
    var
        Customer: Record Customer;
        CustomerMutation: Codeunit "SMC Customer Mutation";
        CustomerNo: Code[20];
        OriginalName: Text[100];
    begin
        // Arrange
        CustomerNo := CreateTestCustomer();
        Customer.Get(CustomerNo);
        OriginalName := Customer.Name;

        // Act
        CustomerMutation.Init(CustomerNo);
        CustomerMutation.When(false).SetName('Should Not Change');
        CustomerMutation.Apply();

        // Assert
        Customer.Get(CustomerNo);
        Assert.AreEqual(OriginalName, Customer.Name, 'Name should not change when condition is false');

        // Cleanup
        Customer.Delete();
    end;

    [Test]
    procedure TestCustomerMutation_MultipleWhenBlocks_ShouldApplyCorrectly()
    var
        Customer: Record Customer;
        CustomerMutation: Codeunit "SMC Customer Mutation";
        CustomerNo: Code[20];
        OriginalName: Text[100];
    begin
        // Arrange
        CustomerNo := CreateTestCustomer();
        Customer.Get(CustomerNo);
        OriginalName := Customer.Name;

        // Act - Apply changes conditionally
        CustomerMutation.Init(CustomerNo);
        CustomerMutation.When(true).SetAddress('123 Main St');
        CustomerMutation.When(false).SetName('Should Not Change');
        CustomerMutation.When(true).SetCity('Seattle');
        CustomerMutation.Apply();

        // Assert
        Customer.Get(CustomerNo);
        Assert.AreEqual('123 Main St', Customer.Address, 'Address should be updated (condition true)');
        Assert.AreEqual(OriginalName, Customer.Name, 'Name should not change (condition false)');
        Assert.AreEqual('Seattle', Customer.City, 'City should be updated (condition true)');

        // Cleanup
        Customer.Delete();
    end;

    [Test]
    procedure TestCustomerMutation_WhenToggle_ShouldRespectLastCondition()
    var
        Customer: Record Customer;
        CustomerMutation: Codeunit "SMC Customer Mutation";
        CustomerNo: Code[20];
        OriginalAddress: Text[100];
    begin
        // Arrange
        CustomerNo := CreateTestCustomer();
        Customer.Get(CustomerNo);
        OriginalAddress := Customer.Address;

        // Act - Toggle condition: true -> false -> true
        CustomerMutation.Init(CustomerNo);
        CustomerMutation.When(true).SetName('Updated Name');
        CustomerMutation.When(false).SetAddress('Should Not Change');
        CustomerMutation.When(true).SetCity('New City');
        CustomerMutation.Apply();

        // Assert
        Customer.Get(CustomerNo);
        Assert.AreEqual('Updated Name', Customer.Name, 'Name should be updated (first When(true))');
        Assert.AreEqual(OriginalAddress, Customer.Address, 'Address should not change (When(false))');
        Assert.AreEqual('New City', Customer.City, 'City should be updated (last When(true))');

        // Cleanup
        Customer.Delete();
    end;

    [Test]
    procedure TestCustomerMutation_ValidateField_ShouldTriggerValidation()
    var
        Customer: Record Customer;
        CustomerMutation: Codeunit "SMC Customer Mutation";
        CustomerNo: Code[20];
    begin
        // Arrange
        CustomerNo := CreateTestCustomer();

        // Act - Use ValidateField instead of Set
        CustomerMutation.Init(CustomerNo);
        CustomerMutation.ValidateField(Customer.FieldNo(Name), 'Validated Name');
        CustomerMutation.Apply();

        // Assert
        Customer.Get(CustomerNo);
        Assert.AreEqual('Validated Name', Customer.Name, 'Name should be set via validation');

        // Cleanup
        Customer.Delete();
    end;

    [Test]
    procedure TestCustomerMutation_ValidateMultipleFields_ShouldUpdateAll()
    var
        Customer: Record Customer;
        CustomerMutation: Codeunit "SMC Customer Mutation";
        CustomerNo: Code[20];
    begin
        // Arrange
        CustomerNo := CreateTestCustomer();

        // Act - Validate multiple fields
        CustomerMutation.Init(CustomerNo);
        CustomerMutation.ValidateField(Customer.FieldNo(Name), 'Validated Customer');
        CustomerMutation.ValidateField(Customer.FieldNo(Address), '456 Oak Ave');
        CustomerMutation.ValidateField(Customer.FieldNo(City), 'Portland');
        CustomerMutation.Apply();

        // Assert
        Customer.Get(CustomerNo);
        Assert.AreEqual('Validated Customer', Customer.Name, 'Name should be validated');
        Assert.AreEqual('456 Oak Ave', Customer.Address, 'Address should be validated');
        Assert.AreEqual('Portland', Customer.City, 'City should be validated');

        // Cleanup
        Customer.Delete();
    end;

    [Test]
    procedure TestCustomerMutation_MixSetAndValidate_ShouldApplyBoth()
    var
        Customer: Record Customer;
        CustomerMutation: Codeunit "SMC Customer Mutation";
        CustomerNo: Code[20];
    begin
        // Arrange
        CustomerNo := CreateTestCustomer();

        // Act - Mix Set and ValidateField in the same chain
        CustomerMutation.Init(CustomerNo);
        CustomerMutation.SetName('Direct Set Name');
        CustomerMutation.ValidateField(Customer.FieldNo(Address), '789 Elm St');
        CustomerMutation.SetCity('Boston');
        CustomerMutation.Apply();

        // Assert
        Customer.Get(CustomerNo);
        Assert.AreEqual('Direct Set Name', Customer.Name, 'Name should be set directly');
        Assert.AreEqual('789 Elm St', Customer.Address, 'Address should be validated');
        Assert.AreEqual('Boston', Customer.City, 'City should be set directly');

        // Cleanup
        Customer.Delete();
    end;

    [Test]
    procedure TestCustomerMutation_ValidateWithWhen_ShouldRespectCondition()
    var
        Customer: Record Customer;
        CustomerMutation: Codeunit "SMC Customer Mutation";
        CustomerNo: Code[20];
        OriginalAddress: Text[100];
    begin
        // Arrange
        CustomerNo := CreateTestCustomer();
        Customer.Get(CustomerNo);
        OriginalAddress := Customer.Address;

        // Act - ValidateField with When conditions
        CustomerMutation.Init(CustomerNo);
        CustomerMutation.When(true).ValidateField(Customer.FieldNo(Name), 'Should Update');
        CustomerMutation.When(false).ValidateField(Customer.FieldNo(Address), 'Should Not Update');
        CustomerMutation.Apply();

        // Assert
        Customer.Get(CustomerNo);
        Assert.AreEqual('Should Update', Customer.Name, 'Name should be validated (condition true)');
        Assert.AreEqual(OriginalAddress, Customer.Address, 'Address should not change (condition false)');

        // Cleanup
        Customer.Delete();
    end;

    [Test]
    procedure TestCustomerMutation_InitWithInvalidCustomer_ShouldThrowError()
    var
        CustomerMutation: Codeunit "SMC Customer Mutation";
        ErrorOccurred: Boolean;
    begin
        // Act & Assert
        asserterror CustomerMutation.Init('NONEXISTENT999');
        ErrorOccurred := GetLastErrorText() <> '';
        Assert.IsTrue(ErrorOccurred, 'Init should throw error for non-existent customer');
    end;

    [Test]
    procedure TestCustomerMutation_ApplyWithoutInit_ShouldReturnFalse()
    var
        CustomerMutation: Codeunit "SMC Customer Mutation";
        ApplyResult: Boolean;
    begin
        // Act - Try to apply without initialization
        ApplyResult := CustomerMutation.Apply();

        // Assert
        Assert.IsFalse(ApplyResult, 'Apply should return false when not initialized');
    end;

    local procedure CreateTestCustomer(): Code[20]
    var
        Customer: Record Customer;
    begin
        Customer.Init();
        Customer."No." := 'TEST-' + Format(Random(99999));
        Customer.Name := 'Test Customer';
        Customer.Insert(true);
        exit(Customer."No.");
    end;
}
