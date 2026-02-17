# BusinessCentral.AL.Mutations - Claude Development Guide

## Autonomous Development Mode

This repository is developed autonomously by Claude. Humans interact through:

- **GitHub Issues** - Feature requests, bug reports, questions
- **Issue Comments** - Feedback, clarifications, approvals
- **PR Reviews** - Code review, approval/rejection

## Project Context

Read these files for full context:
- `CONCEPT.md` - Product vision and goals (mutation testing tool)
- `docs/ARCHITECTURE.md` - Technical design, execution flow, project structure

## IMPORTANT: Project Pivot

This project was originally built as an AL data mutation library (fluent API for modifying
BC records). That was WRONG. The project is a **mutation testing tool** - an external PowerShell
tool that modifies AL source code to validate test quality.

The existing AL code in `src/`, `tests/`, `app.json`, etc. is from the old wrong direction
and must be **removed** as part of the pivot. See the architecture docs for the correct
project structure.

## What This Tool Does

1. Takes a user's existing AL project with source code and tests
2. Scans `.al` source files for mutable code locations
3. Creates a BC container, compiles and tests the original code (baseline)
4. For each mutation: modifies source -> compiles -> deploys -> runs tests -> restores
5. Reports which mutations survived (test gaps) vs were killed (tests caught them)

## Project Structure (Target)

```
BCMutations/                  # PowerShell module
  BCMutations.psd1            # Module manifest
  BCMutations.psm1            # Module loader
  Public/                     # Exported functions
    Invoke-BCMutationTest.ps1 # Main entry point
    New-BCMutationConfig.ps1  # Generate default config
    Get-BCMutationOperators.ps1
  Private/                    # Internal functions
    Find-MutationTargets.ps1
    New-Mutation.ps1
    Restore-Mutation.ps1
    Invoke-ContainerSetup.ps1
    Invoke-ContainerTeardown.ps1
    Invoke-AppCompile.ps1
    Invoke-AppDeploy.ps1
    Invoke-TestRun.ps1
    ConvertTo-MutationReport.ps1
    Test-LineContext.ps1
    Read-OperatorFile.ps1
    Write-MutationProgress.ps1
operators/
  default.json                # Default mutation operators (JSON token pairs)
  schema.json                 # JSON Schema for operator files
action.yml                    # GitHub Action definition
entrypoint.ps1                # GitHub Action entrypoint
tests/
  Unit/                       # Pester unit tests (no BC container needed)
  Integration/                # Pester integration tests (mocked container)
  Fixtures/                   # Sample AL files and operator files
docs/
  USAGE.md                    # How to use the tool
  OPERATORS.md                # How to write custom operators
```

## Files to Remove (Old AL Code)

These are from the old wrong direction and should be deleted:
- `src/` directory (all AL source code)
- `tests/Codeunits/` (AL tests)
- `app.json` (AL manifest - this repo is a tool, not an AL app)
- `.vscode/` (AL dev config)
- `docs/API.md` (describes old product)
- `docs/EXAMPLES.md` (describes old product)
- `docs/TOOLING.md` (reference material, superseded by ARCHITECTURE.md)

## Bootstrap Priority

1. **Remove old AL code** - Delete files listed above
2. **Module structure** - Create `BCMutations/` with manifest, loader, Public/Private folders
3. **Operators** - Create `operators/default.json` with AL mutation operators
4. **Core functions** - Implement private functions with Pester tests (TDD)
   - Start with: `Read-OperatorFile`, `Test-LineContext`, `Find-MutationTargets`
   - Then: `New-Mutation`, `Restore-Mutation`
   - Then: Container functions (`Invoke-ContainerSetup`, etc.)
5. **Main orchestrator** - `Invoke-BCMutationTest`
6. **Reporting** - `ConvertTo-MutationReport`
7. **GitHub Action** - `action.yml` + `entrypoint.ps1`
8. **CI** - `.github/workflows/ci.yml` for Pester tests

## Development Process

### For Each Issue:

1. **Acknowledge** - Comment that you're starting work
2. **Plan** - Break into subtasks if complex
3. **Branch** - Create feature branch from master
4. **TDD Loop**:
   - Write failing Pester test first
   - Implement minimal PowerShell to pass
   - Verify test passes
   - Refactor if needed
   - Commit with clear message
5. **PR** - Create pull request with summary
6. **Respond** - Answer any review comments
7. **Close** - After merge, close related issue

### Commit Message Format

```
type(scope): description

- detail 1
- detail 2

Refs #issue-number
```

Types: `feat`, `fix`, `test`, `docs`, `refactor`, `chore`

### PowerShell Standards

- Use approved PowerShell verbs (Get, Set, New, Invoke, etc.)
- Public functions: `Verb-BCMutation*` naming
- Follow PowerShell module best practices (manifest, loader, Public/Private split)
- Use `[CmdletBinding()]` on all public functions
- Use `[Parameter()]` attributes with proper validation
- Write Pester tests for every function

### Testing Requirements

- Every public and private function must have Pester tests
- Unit tests run without a BC container (mock BcContainerHelper where needed)
- Test edge cases (empty files, invalid JSON, no matches found)
- Use descriptive test names: `It 'finds mutation targets in AL source file'`

## GitHub CLI Commands

```bash
# Issues
gh issue list
gh issue view <number>
gh issue comment <number> -b "message"
gh issue close <number>

# Pull Requests
gh pr create --title "..." --body "..."
gh pr list
gh pr merge <number>

# Branches
git checkout -b feature/issue-<number>-description
git push -u origin <branch>
```

## Autonomous Behaviors

During scheduled runs, you may:
- Fix failing Pester tests
- Improve documentation
- Refactor for clarity (with tests)
- Close stale issues with explanation

## Forbidden Actions

- Force push to master
- Delete branches without merging
- Ignore failing tests
- Skip code review process
- Commit secrets or credentials
- Merge your own PRs without review (request review first)

## Definition of Done

A task is complete when:
- [ ] PowerShell code implemented
- [ ] Pester tests written and passing
- [ ] Documentation updated
- [ ] PR created and linked to issue
- [ ] Code reviewed (or review requested)
