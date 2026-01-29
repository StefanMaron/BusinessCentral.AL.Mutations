# Tooling & CI/CD Guide

This document provides Claude with the knowledge needed to set up and maintain the CI/CD pipeline.

## BcContainerHelper

Use BcContainerHelper (NOT AL-Go) for CI/CD. It provides better control for parallel container execution.

### Installation in GitHub Actions

```yaml
- name: Install BcContainerHelper
  shell: pwsh
  run: |
    Install-Module BcContainerHelper -Force -AllowClobber
    Import-Module BcContainerHelper
```

### Creating a BC Container

```powershell
$containerName = "bcmutations"
$credential = New-Object PSCredential 'admin', (ConvertTo-SecureString 'P@ssw0rd' -AsPlainText -Force)

New-BcContainer `
    -containerName $containerName `
    -accept_eula `
    -credential $credential `
    -artifactUrl (Get-BCArtifactUrl -type OnPrem -country w1 -select Latest) `
    -auth NavUserPassword `
    -updateHosts `
    -isolation hyperv  # Use 'process' on Windows runners
```

### Compiling AL Code

```powershell
Compile-AppInBcContainer `
    -containerName $containerName `
    -appProjectFolder $env:GITHUB_WORKSPACE `
    -appOutputFolder "$env:GITHUB_WORKSPACE/.output" `
    -credential $credential
```

### Running Tests

```powershell
$testResults = Run-TestsInBcContainer `
    -containerName $containerName `
    -credential $credential `
    -testSuite "DEFAULT" `
    -returnTrueIfAllPassed

if (-not $testResults) {
    throw "Tests failed"
}
```

### Publishing App

```powershell
Publish-BcContainerApp `
    -containerName $containerName `
    -appFile "$env:GITHUB_WORKSPACE/.output/app.app" `
    -skipVerification `
    -sync `
    -install
```

### Cleanup

```powershell
Remove-BcContainer -containerName $containerName
```

## GitHub Actions Runner Requirements

- Use `windows-latest` runner (BC containers require Windows)
- Enable Hyper-V or use process isolation
- Recommended: self-hosted runner for faster builds (container caching)

## Example Workflow Structure

```yaml
name: Build and Test

on:
  push:
    branches: [main]
    paths:
      - 'src/**'
      - 'tests/**'
      - 'app.json'
  pull_request:
    branches: [main]

jobs:
  build:
    runs-on: windows-latest
    steps:
      - uses: actions/checkout@v4

      - name: Install BcContainerHelper
        shell: pwsh
        run: |
          Install-Module BcContainerHelper -Force
          Import-Module BcContainerHelper

      - name: Create BC Container
        shell: pwsh
        run: |
          # Container creation script

      - name: Compile
        shell: pwsh
        run: |
          # Compilation script

      - name: Run Tests
        shell: pwsh
        run: |
          # Test execution script

      - name: Cleanup
        if: always()
        shell: pwsh
        run: |
          Remove-BcContainer -containerName "bcmutations" -ErrorAction SilentlyContinue
```

## AL Project Structure

### Required Files

```
app.json              # App manifest with dependencies
src/
  Codeunits/         # Implementation
  Interfaces/        # Interface definitions
tests/
  Codeunits/         # Test codeunits with [Test] methods
.vscode/
  launch.json        # For local development
  settings.json      # AL extension settings
```

### Test Codeunit Pattern

```al
codeunit 50110 "SMC Mutation Tests"
{
    Subtype = Test;

    [Test]
    procedure TestCustomerMutation_WhenSettingName_ShouldUpdateRecord()
    var
        Customer: Record Customer;
        Mutation: Codeunit "SMC Customer Mutation";
    begin
        // Arrange
        LibraryCustomer.CreateCustomer(Customer);

        // Act
        Mutation.Init(Customer."No.");
        Mutation.Set(Customer.FieldNo(Name), 'Test Name');
        Mutation.Apply();

        // Assert
        Customer.Get(Customer."No.");
        Assert.AreEqual('Test Name', Customer.Name, 'Name should be updated');
    end;
}
```

## References

For more examples, study these repositories:
- https://github.com/StefanMaron/BusinessCentral.LinterCop (uses BcContainerHelper)
- https://github.com/microsoft/BCApps (Microsoft's approach)
- https://github.com/microsoft/AL-Go (alternative, more complex)
