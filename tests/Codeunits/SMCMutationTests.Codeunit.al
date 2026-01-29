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
