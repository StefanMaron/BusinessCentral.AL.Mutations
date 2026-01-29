# BusinessCentral.AL.Mutations - Claude Development Guide

## Autonomous Development Mode

This repository is developed autonomously by Claude. Humans interact through:

- **GitHub Issues** - Feature requests, bug reports, questions
- **Issue Comments** - Feedback, clarifications, approvals
- **PR Reviews** - Code review, approval/rejection

## Project Context

Read these files for full context:
- `CONCEPT.md` - Product vision and goals
- `docs/ARCHITECTURE.md` - Technical design decisions
- `docs/API.md` - API documentation (generated)

## Project Structure

```
src/
  Codeunits/         # Core mutation codeunits
  Interfaces/        # Interface definitions
  Enums/             # Enum types
  Tables/            # Helper tables (if needed)
tests/
  Codeunits/         # Test codeunits
  Helpers/           # Test utilities
docs/
  ARCHITECTURE.md    # Design decisions
  API.md             # API documentation
  EXAMPLES.md        # Usage examples
app.json             # AL project manifest
.github/
  workflows/         # CI/CD and Claude agent
  prompts/           # Agent prompts
```

## Development Process

### For Each Issue:

1. **Acknowledge** - Comment that you're starting work
2. **Plan** - Break into subtasks if complex
3. **Branch** - Create feature branch from main
4. **TDD Loop**:
   - Write failing test first
   - Implement minimal code to pass
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

### AL Code Standards

- Follow Microsoft AL best practices
- Use meaningful variable names (no single letters except loop counters)
- Document all public procedures with XML comments
- Keep procedures focused and small
- Use interfaces for extensibility
- Avoid global variables

### Testing Requirements

- Every public procedure must have tests
- Test edge cases (empty, null, boundary values)
- Use descriptive test method names: `TestCustomerMutation_WhenNameChanged_ShouldUpdateRecord`
- Minimum 90% code coverage goal

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

## Communication Style

When commenting on issues:
- Be concise but thorough
- Use code blocks for AL examples
- Link to relevant files with line numbers
- Ask clarifying questions if requirements unclear
- Provide progress updates on longer tasks

## Autonomous Behaviors

During scheduled runs, you may:
- Fix failing tests
- Improve documentation
- Refactor for clarity (with tests)
- Close stale issues with explanation
- Update dependencies in app.json

## Forbidden Actions

- Force push to main
- Delete branches without merging
- Ignore failing tests
- Skip code review process
- Commit secrets or credentials
- Merge your own PRs without review (request review first)

## Issue Labels

Use these labels when creating/updating issues:
- `feature` - New functionality
- `bug` - Something broken
- `docs` - Documentation updates
- `test` - Test improvements
- `refactor` - Code cleanup
- `blocked` - Waiting on something
- `good-first-issue` - Simple, good for starting

## Definition of Done

A task is complete when:
- [ ] Code implemented and compiles
- [ ] Tests written and passing
- [ ] Documentation updated
- [ ] PR created and linked to issue
- [ ] Code reviewed (or review requested)
