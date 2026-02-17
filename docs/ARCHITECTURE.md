# Architecture

## Overview

BCMutations is a PowerShell module that performs mutation testing on Business Central AL code.
It modifies AL source files, compiles and tests them in a BC container, and reports which
mutations were detected by the test suite.

## Design Principles

1. **Simple Operators** - Mutations defined as JSON token pairs, engine handles context
2. **Single Container** - One expensive container setup, fast inner loop
3. **Minimal Dependencies** - Only BcContainerHelper (checked at runtime)
4. **PowerShell Conventions** - Approved verbs, module manifest, Public/Private split

## Project Structure

```
BCMutations/                          # PowerShell module
  BCMutations.psd1                    # Module manifest
  BCMutations.psm1                    # Module loader
  Public/                             # Exported functions
    Invoke-BCMutationTest.ps1         # Main entry point
    New-BCMutationConfig.ps1          # Generate default config
    Get-BCMutationOperators.ps1       # List available operators
  Private/                            # Internal functions
    Find-MutationTargets.ps1          # Scan .al files for mutation sites
    New-Mutation.ps1                  # Apply a mutation to a file
    Restore-Mutation.ps1              # Restore original file
    Invoke-ContainerSetup.ps1         # Create BC container
    Invoke-ContainerTeardown.ps1      # Remove BC container
    Invoke-AppCompile.ps1             # Compile AL app
    Invoke-AppDeploy.ps1              # Publish + install app
    Invoke-TestRun.ps1                # Run tests, return pass/fail
    ConvertTo-MutationReport.ps1      # Generate report
    Test-LineContext.ps1              # Check if match is in comment/string
    Read-OperatorFile.ps1             # Load and validate operator JSON
    Write-MutationProgress.ps1        # Console progress output
operators/
  default.json                        # Default AL mutation operators
  schema.json                         # JSON Schema for operator files
action.yml                            # GitHub Action definition
entrypoint.ps1                        # GitHub Action entrypoint
tests/
  Unit/                               # Pester unit tests
  Integration/                        # Pester integration tests (mocked container)
  Fixtures/                           # Test data (sample AL files, operator files)
```

## Execution Flow

```
PHASE 1: INITIALIZATION
  - Validate ProjectPath contains app.json
  - Load and validate operator definitions
  - Check BcContainerHelper is available

PHASE 2: MUTATION DISCOVERY
  - Scan .al files in SourceFolder
  - For each file + line + operator: check context (skip comments/strings)
  - Record mutation candidates: { File, Line, Operator, Column }
  - If -DryRun: output list and return

PHASE 3: CONTAINER SETUP
  - Create BC container with test toolkit (New-BcContainer)
  - Volume mount user's project folder

PHASE 4: BASELINE
  - Compile original app (Compile-AppInBcContainer)
  - Deploy to container (Publish-BcContainerApp)
  - Run tests (Run-TestsInBcContainer)
  - Abort if baseline tests fail

PHASE 5: MUTATION LOOP
  For each mutation candidate:
    1. Apply mutation to .al file
    2. Compile (if fail -> CompileError, skip)
    3. Deploy to container
    4. Run tests
       - Tests fail -> Killed (good)
       - Tests pass -> Survived (test gap)
    5. Restore original file

PHASE 6: TEARDOWN
  - Remove BC container

PHASE 7: REPORT
  - Calculate score: killed / (killed + survived)
  - Generate report (JSON/Markdown)
  - Output summary to console
```

## Mutation Operator Schema

Operators are simple token pairs defined in JSON:

```json
{
  "operators": [
    {
      "id": "rel-gt-to-gte",
      "name": "Greater-than to greater-or-equal",
      "category": "relational",
      "pattern": " > ",
      "replacement": " >= "
    }
  ]
}
```

- `pattern`: Literal string to find in AL source lines
- `replacement`: String to substitute. `null` means comment out the entire line
- `id`: Unique kebab-case identifier for filtering and reporting
- `category`: Grouping for filtering and report organization

The engine handles context filtering (comments, strings), not the operator file.

## Context Detection (Test-LineContext)

Simple state machine that determines if a character position is inside:
- Single-line comment (`//` to end of line)
- Block comment (`/* ... */`)
- String literal (`'...'` in AL)

This covers 99% of real AL code without needing a full parser.

## Container Lifecycle

Container creation is the most expensive operation (~5-10 min). The tool creates one container
and reuses it for all mutations:

- `New-BcContainer` with `-includeTestToolkit -includeTestLibrariesOnly`
- Volume mount the project folder for source access
- Each mutant: compile -> deploy (via `-useDevEndpoint`) -> test -> restore
- `Remove-BcContainer` at the end (in `finally` block)

## Version Bumping

BC won't install an app with the same version. For each mutant, the tool increments the build
number in `app.json` (e.g., `1.0.0.0` -> `1.0.0.1` -> `1.0.0.2`) and restores it after.

## Report Format

The tool generates reports in JSON (default) and Markdown:

- **Mutation score**: killed / (killed + survived). CompileErrors are excluded.
- **Survived mutants**: Listed with file, line, original code, mutated code
- **Killed mutants**: Summary count (details in expandable section)

## Key Parameters

| Parameter | Description | Default |
|---|---|---|
| `-ProjectPath` | User's AL project root | (required) |
| `-SourceFolder` | Subfolder with .al source files | `src` |
| `-OperatorFile` | Custom operator JSON | bundled `default.json` |
| `-ContainerName` | BC container name | `bcmutations` |
| `-TestSuite` | Test suite to run | `DEFAULT` |
| `-ReportFormat` | `json` or `markdown` | `json` |
| `-DryRun` | List mutations without executing | `false` |
| `-SkipContainerCreate` | Use existing container | `false` |
| `-SkipContainerRemove` | Leave container after run | `false` |
| `-MaxMutants` | Limit mutant count (0=unlimited) | `0` |

## Testing Strategy

- **Unit tests** (Pester): Test each private function in isolation. No BC container needed.
  Run on any OS (ubuntu-latest in CI).
- **Integration tests** (Pester): Test `Invoke-BCMutationTest` with mocked BcContainerHelper.
  Verify the orchestration logic without a real container.
- **End-to-end**: Requires Windows + Docker. Manual or on self-hosted runner.
