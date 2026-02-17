# BCMutations Usage Guide

BCMutations is a PowerShell mutation testing tool for Business Central AL code.
It validates your test suite quality by introducing small code changes and checking if your tests catch them.

## Prerequisites

- PowerShell 5.1 or later (or PowerShell 7+)
- [BcContainerHelper](https://github.com/microsoft/navcontainerhelper) module installed
- Docker (for running BC containers)
- A BC AL project with a test suite

## Installation

```powershell
# Clone the repository
git clone https://github.com/StefanMaron/BusinessCentral.AL.Mutations.git

# Import the module
Import-Module ./BusinessCentral.AL.Mutations/BCMutations/BCMutations.psd1
```

## Quick Start

```powershell
# Run mutation testing on your AL project
Invoke-BCMutationTest -ProjectPath ./MyBCExtension

# Output:
# === BCMutations: Mutation Testing Tool ===
# Loading operators from: .../operators/default.json
# Scanning AL files in: ./MyBCExtension/src
# Found 42 mutation candidates across 3 file(s).
# Creating BC container: bcmutations
# Compiling baseline...
# Running baseline tests...
# Baseline: OK
# Starting mutation loop (42 mutants)...
# [1/42 2%] [KILLED] rel-gt-to-gte @ MyCodeunit.al:15
# [2/42 5%] [SURVIVED] logic-and-to-or @ MyCodeunit.al:23
# ...
# === Mutation Testing Complete ===
# Mutation Score: 75.0% (32 killed, 10 survived)
# Report written to: ./MyBCExtension/mutation-report/report.json
```

## Commands

### Invoke-BCMutationTest

Main entry point. Runs the full mutation testing loop.

```powershell
Invoke-BCMutationTest
    -ProjectPath <string>         # Required: path to AL project root
    [-SourceFolder <string>]      # Source subfolder (default: 'src')
    [-OperatorFile <string>]      # Custom operator file (default: built-in)
    [-ContainerName <string>]     # BC container name (default: 'bcmutations')
    [-TestSuite <string>]         # Test suite name (default: 'DEFAULT')
    [-ReportFormat <json|markdown>]  # Report format (default: 'json')
    [-ReportPath <string>]        # Report output path
    [-DryRun]                     # List mutations without running
    [-SkipContainerCreate]        # Use existing container
    [-SkipContainerRemove]        # Leave container after run
    [-MaxMutants <int>]           # Limit mutant count (0 = unlimited)
    [-ArtifactUrl <string>]       # BC artifact URL
```

**Example: Dry run to see what mutations would be tested**
```powershell
Invoke-BCMutationTest -ProjectPath ./MyBCExtension -DryRun
```

**Example: Use existing container, limit to 20 mutants**
```powershell
Invoke-BCMutationTest -ProjectPath ./MyBCExtension -SkipContainerCreate -MaxMutants 20
```

**Example: Generate Markdown report**
```powershell
Invoke-BCMutationTest -ProjectPath ./MyBCExtension -ReportFormat markdown
```

### Get-BCMutationOperators

Lists available mutation operators.

```powershell
# List all operators
Get-BCMutationOperators

# Filter by category
Get-BCMutationOperators -Category relational

# From a custom operator file
Get-BCMutationOperators -OperatorFile ./my-operators.json
```

### New-BCMutationConfig

Generates a default configuration file.

```powershell
New-BCMutationConfig
New-BCMutationConfig -OutputPath ./config/bcmutations.json
```

## Understanding Results

### Mutation Score

`Mutation Score = Killed / (Killed + Survived) * 100`

- **Killed**: Your tests detected the code change (good - your tests are effective)
- **Survived**: Your tests didn't detect the change (bad - this is a test gap)
- **Compile Error**: The mutation caused a compile error (not counted in score)

A score of 100% means every mutation was caught by your tests. A score of 50% means half of the mutations survived - you have test gaps.

### Report

The JSON report includes:
- `mutationScore`: Overall score as a percentage
- `killed`, `survived`, `compileErrors`, `total`: Counts
- `survivors`: Details of each survived mutation (file, line, operator)

The Markdown report includes the same information in a readable format with a summary table and a list of survived mutants.

## GitHub Action

Add BCMutations to your GitHub Actions workflow:

```yaml
- name: Run Mutation Testing
  uses: StefanMaron/BusinessCentral.AL.Mutations@main
  with:
    project-path: './MyBCExtension'
    source-folder: 'src'
    test-suite: 'DEFAULT'
    report-format: 'json'

- name: Upload Report
  uses: actions/upload-artifact@v4
  with:
    name: mutation-report
    path: mutation-report/
```

## Tips

1. **Start with dry run**: Use `-DryRun` to see how many mutations will be tested
2. **Use MaxMutants for CI**: Limit mutations with `-MaxMutants 50` for faster CI
3. **Focus on categories**: Use a custom operator file to test only specific operator categories
4. **Fix survivors first**: Prioritize writing tests for survived mutations - these are your biggest test gaps
