# C# Migration Plan: BusinessCentral.AL.Mutations

## 1. Solution Structure

### Project Layout

```
AlMutate/
  AlMutate.slnx
  AlMutate/
    AlMutate.csproj
    Program.cs                    # CLI entry point
    MutationPipeline.cs           # Orchestrator (scan/run/replay)
    Models/
      MutationCandidate.cs        # Scan output record
      MutationResult.cs           # Execution outcome record
      MutationOperator.cs         # Deserialized operator JSON
      OperatorFile.cs             # Root JSON container
      MutationLogFile.cs          # mutations.json root
      MutationRun.cs              # Single run within log
      PipelineOptions.cs          # CLI-parsed options
      PipelineResult.cs           # Pipeline return value
    Services/
      OperatorLoader.cs           # Load + validate operator JSON
      AlScanner.cs                # Scan .al files for candidates
      Mutator.cs                  # Apply/restore mutations
      TestRunner.cs               # Run tests via AL Runner
      MutationLog.cs              # Read/write mutations.json
      ReportGenerator.cs          # Markdown report
      GitService.cs               # Git working tree check + restore
    operators/
      default.json                # Embedded resource (copied from current)
      schema.json                 # Embedded resource
  AlMutate.Tests/
    AlMutate.Tests.csproj
    CliRunner.cs                  # Spawn al-mutate as child process
    OperatorLoaderTests.cs
    AlScannerTests.cs
    MutatorTests.cs
    MutationLogTests.cs
    ReportGeneratorTests.cs
    TestRunnerTests.cs
    PipelineTests.cs
    CliTests.cs
    fixtures/
      sample.al
      NoMatches.al
  tests/                          # End-to-end fixture directories
    01-relational/
      src/*.al
      test/*.al
    02-arithmetic/
      src/*.al
      test/*.al
    03-statement-removal/
      src/*.al
      test/*.al
```

### NuGet Package Metadata (AlMutate.csproj)

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <OutputType>Exe</OutputType>
    <TargetFramework>net8.0</TargetFramework>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>

    <PackAsTool>true</PackAsTool>
    <ToolCommandName>al-mutate</ToolCommandName>
    <PackageId>MSDyn365BC.AL.Mutate</PackageId>
    <Version>0.1.0</Version>
    <Description>Mutation testing tool for Business Central AL code</Description>
    <Authors>Stefan Maron</Authors>
    <PackageLicenseExpression>MIT</PackageLicenseExpression>
    <PackageProjectUrl>https://github.com/StefanMaron/BusinessCentral.AL.Mutations</PackageProjectUrl>
    <RepositoryUrl>https://github.com/StefanMaron/BusinessCentral.AL.Mutations</RepositoryUrl>
    <PackageTags>business-central;al;mutation-testing</PackageTags>
    <InternalsVisibleTo>AlMutate.Tests</InternalsVisibleTo>
  </PropertyGroup>

  <ItemGroup>
    <EmbeddedResource Include="operators/default.json" />
    <EmbeddedResource Include="operators/schema.json" />
  </ItemGroup>

  <ItemGroup>
    <PackageReference Include="System.CommandLine" Version="2.0.0-beta4.22272.1" />
  </ItemGroup>
</Project>
```

### Dependencies

| Package | Purpose |
|---------|---------|
| `System.CommandLine` (beta4) | CLI parsing (scan/run/replay subcommands) |
| `System.Text.Json` | Operator JSON + mutations.json (built into net8.0) |
| `MSDyn365BC.AL.Runner` (project reference or NuGet) | AL compilation + test execution |

The test project uses xUnit, matching AL Runner:

```xml
<PackageReference Include="Microsoft.NET.Test.Sdk" Version="17.8.0" />
<PackageReference Include="xunit" Version="2.9.3" />
<PackageReference Include="xunit.runner.visualstudio" Version="2.8.2" />
```

No tree-sitter dependency. No Python dependency. No PowerShell dependency.

---

## 2. AL AST Scanning Strategy

### Decision: Use AL Runner's AL Compiler for AST Access

The Python tool used tree-sitter-al (a tree-sitter grammar) for AST scanning. For C#, there are four options:

| Option | Pros | Cons |
|--------|------|------|
| (a) P/Invoke tree-sitter-al native lib | Same logic as Python | Requires native library distribution per platform, C interop maintenance, no existing .NET binding |
| (b) AL Compiler AST via `Microsoft.Dynamics.Nav.CodeAnalysis` | First-party AST, identical to what the compiler uses, already referenced by AL Runner | Requires AL compiler DLLs (auto-downloaded by AL Runner's build), API is undocumented |
| (c) AL Runner `--dump-csharp` then scan Roslyn AST | Uses existing tooling | Loses AL-level node types, mutations would target C# not AL source |
| (d) Regex/text-based | No dependencies | Fragile, cannot distinguish code vs comments/strings, last resort |

**Recommendation: Option (b) -- use `Microsoft.Dynamics.Nav.CodeAnalysis` directly.**

AL Runner already references `Microsoft.Dynamics.Nav.CodeAnalysis.dll` and `Microsoft.Dynamics.Nav.CodeAnalysis.Workspaces.dll`. These assemblies contain a full Roslyn-style API for parsing AL:

- `NavSyntaxTree.ParseObjectText(string)` -- parse AL source into a syntax tree
- Syntax node types like `IfStatementSyntax`, `BinaryExpressionSyntax`, `InvocationExpressionSyntax` map directly to the tree-sitter node types the Python code targets
- Comment and string literal nodes are distinct syntax kinds -- same safety as tree-sitter
- The AL compiler DLLs are auto-downloaded by AL Runner's MSBuild targets (no manual setup)

The `AlMutate.csproj` will reference the same AL compiler DLLs as AL Runner, using the same `EnsureALCompiler` MSBuild target pattern. This means:

1. `AlScanner` walks the `NavSyntaxTree` using a `CSharpSyntaxWalker`-style visitor
2. For each node matching an operator's target (e.g., `BinaryExpressionSyntax` with `>` operator), it creates a `MutationCandidate`
3. The candidate records the file path, 1-based line number, original line text, and mutated line text -- identical output to the Python scanner

AL Runner's build already handles cross-platform download of the AL compiler NuGet package. AlMutate will use the same `_ALToolVersion` property and `EnsureALCompiler` target.

### Scanner Implementation Sketch

```csharp
using Microsoft.Dynamics.Nav.CodeAnalysis;
using Microsoft.Dynamics.Nav.CodeAnalysis.Syntax;

internal static class AlScanner
{
    public static List<MutationCandidate> ScanFile(string filePath, List<MutationOperator> operators)
    {
        var source = File.ReadAllText(filePath);
        var tree = NavSyntaxTree.ParseObjectText(source);
        var root = tree.GetRoot();
        var lines = source.Split('\n');

        var candidates = new List<MutationCandidate>();
        var visitor = new MutationVisitor(filePath, lines, operators, candidates);
        visitor.Visit(root);
        return candidates;
    }

    public static List<MutationCandidate> ScanDirectory(string directory, List<MutationOperator> operators)
    {
        var candidates = new List<MutationCandidate>();
        foreach (var file in Directory.GetFiles(directory, "*.al", SearchOption.AllDirectories).Order())
            candidates.AddRange(ScanFile(file, operators));
        return candidates;
    }
}
```

The `MutationVisitor` extends `NavSyntaxWalker` and overrides `VisitBinaryExpression`, `VisitInvocationExpression`, `VisitUnaryExpression`, etc. Each visit method checks whether the node is inside a method body (not in property declarations or attributes) and matches against the operator list.

If `Microsoft.Dynamics.Nav.CodeAnalysis` proves too difficult to work with (undocumented API surface), fall back to option (a) with a `TreeSitter.Bindings` NuGet package wrapping the native tree-sitter-al library. But start with option (b) since AL Runner already proves these DLLs work.

---

## 3. Module Breakdown

### 3.1 Models (Value Objects)

All models are `record` types (immutable, value equality).

```csharp
// Deserialized from operators/default.json
public record MutationOperator(
    string Id,
    string Name,
    string Category,
    string NodeType,
    string? OperatorToken,
    string? Identifier,
    string? ArgumentMatch,
    string? IdentifierReplacement,
    string? Replacement)
{
    public bool IsStatementRemoval => Replacement is null && IdentifierReplacement is null;
}

public record OperatorFile(MutationOperator[] Operators);

// Output of scanning
public record MutationCandidate(
    string File,
    int Line,
    string OperatorId,
    string Original,
    string Mutated);

// Result of executing one mutation
public record MutationResult(
    string Id,
    string Operator,
    string File,
    int Line,
    string Original,
    string Mutated,
    MutationStatus Status,
    string? CaughtBy);

public enum MutationStatus { Killed, Survived, CompileError, Obsolete }

// CLI options
public record PipelineOptions
{
    public string Command { get; init; } = "";           // scan | run | replay
    public string SourcePath { get; init; } = "";
    public string? TestPath { get; init; }
    public string? OperatorsPath { get; init; }
    public string? LogFilePath { get; init; }
    public int? MaxMutations { get; init; }
}

// Pipeline output
public record PipelineResult(
    int ExitCode,
    List<MutationResult> Results,
    double Score,
    string? ReportMarkdown);
```

### 3.2 OperatorLoader

```csharp
internal static class OperatorLoader
{
    // Load from file path or embedded resource
    public static List<MutationOperator> Load(string? path = null);
    public static List<string> Validate(JsonDocument doc);
    internal static List<MutationOperator> LoadFromStream(Stream stream);
}
```

- **Input:** File path (or null for embedded default)
- **Output:** `List<MutationOperator>`
- **Validation:** Check required fields, valid categories, unique IDs. Use `System.Text.Json` deserialization with `JsonPropertyName` attributes. Schema validation via manual checks (no external JSON Schema library needed -- keep dependencies minimal).
- **Tests:** `OperatorLoaderTests` -- same assertions as Python `test_operators.py`

### 3.3 AlScanner

```csharp
internal static class AlScanner
{
    public static List<MutationCandidate> ScanFile(string filePath, List<MutationOperator> operators);
    public static List<MutationCandidate> ScanDirectory(string directory, List<MutationOperator> operators);
}
```

- **Input:** AL file path + loaded operators
- **Output:** `List<MutationCandidate>`
- **Logic:** Parse via `NavSyntaxTree`, walk AST, match nodes against operators, build mutated line text
- **Tests:** `AlScannerTests` -- fixture-based, uses `sample.al` and `NoMatches.al`

### 3.4 Mutator

```csharp
internal static class Mutator
{
    public static void Apply(MutationCandidate candidate);
    // Throws MutationException if original line doesn't match
}
```

- **Input:** `MutationCandidate` (has file path, line, original, mutated)
- **Output:** Modifies the file on disk
- **Tests:** `MutatorTests` -- temp file based, verify line replacement, verify exception on mismatch

### 3.5 GitService

```csharp
internal static class GitService
{
    public static bool IsWorkingTreeClean();
    public static void RestoreFile(string filePath);
    // Throws MutationException on failure
}
```

- **Input:** File path
- **Output:** Runs `git checkout -- <file>` or `git status --porcelain`
- **Tests:** Mock `Process` calls or use temp git repos

### 3.6 TestRunner

```csharp
internal class TestRunner
{
    private readonly AlRunnerPipeline _pipeline;

    public TestRunner(AlRunnerPipeline pipeline);

    // Run tests and return (allPassed, failedTestNames)
    public (bool Passed, List<string> FailedTests) RunTests(string sourcePath, string testPath);
}
```

- **Input:** Source and test directories
- **Output:** Pass/fail + list of failed test names (for `caught_by` field)
- **Integration:** Calls `AlRunnerPipeline.Run()` in-process (see section 6)
- **Tests:** `TestRunnerTests` -- mock `AlRunnerPipeline` via interface extraction

### 3.7 MutationLog

```csharp
internal class MutationLog
{
    public static MutationLog Load(string path);
    public static MutationLog Create(string path, string project);

    public void AppendRun(List<MutationResult> results);
    public void Save();
    public List<MutationResult> GetSurvivedFromLastRun();
    public string NextMutationId();
}
```

- **Input/Output:** `mutations.json` file
- **Format:** Identical JSON schema to Python version (schema_version 1)
- **Tests:** `MutationLogTests` -- temp files, verify round-trip, append, survived filtering

### 3.8 ReportGenerator

```csharp
internal static class ReportGenerator
{
    public static double CalculateScore(List<MutationResult> results);
    public static string GenerateMarkdown(List<MutationResult> results, string project);
}
```

- **Input:** Results list + project name
- **Output:** Score (double) and Markdown string
- **Tests:** `ReportGeneratorTests` -- same assertions as Python `test_report.py`

### 3.9 MutationPipeline

```csharp
public class MutationPipeline
{
    public PipelineResult Scan(PipelineOptions options);
    public PipelineResult Run(PipelineOptions options);
    public PipelineResult Replay(PipelineOptions options);
}
```

- Orchestrates all services
- Testable in-process (no CLI needed)
- **Tests:** `PipelineTests` with mocked `TestRunner`

### 3.10 Program.cs (CLI)

```csharp
// Uses System.CommandLine for subcommands
var rootCommand = new RootCommand("Mutation testing tool for Business Central AL code");

var scanCommand = new Command("scan", "List mutation candidates without executing");
scanCommand.AddArgument(new Argument<string>("source", "AL source directory"));
scanCommand.AddOption(new Option<string?>("--operators", "Custom operator JSON"));

var runCommand = new Command("run", "Run full mutation testing");
runCommand.AddArgument(new Argument<string>("source", "AL source directory"));
runCommand.AddOption(new Option<string>("--tests", "Test directory or app path") { IsRequired = true });
runCommand.AddOption(new Option<string?>("--operators", "Custom operator JSON"));
runCommand.AddOption(new Option<int?>("--max", "Max mutations to run"));

var replayCommand = new Command("replay", "Replay survived mutations from a log");
replayCommand.AddArgument(new Argument<string>("log-file", "mutations.json path"));
replayCommand.AddOption(new Option<string>("--tests", "Test directory or app path") { IsRequired = true });
```

---

## 4. TDD Phases

### Phase 1: Operator Loading

**Implement:** `MutationOperator` record, `OperatorFile` record, `OperatorLoader` class.

**Tests written FIRST (10 tests):**

| Test Name | Asserts |
|-----------|---------|
| `LoadDefaultOperators_ReturnsNonEmpty` | `Assert.NotEmpty(operators)` |
| `LoadDefaultOperators_AllHaveRequiredFields` | Every operator has non-empty Id, Name, Category, NodeType |
| `LoadDefaultOperators_AllIdsUnique` | `Assert.Equal(count, distinctCount)` |
| `LoadDefaultOperators_HasExpectedCategories` | relational, arithmetic, logical, statement-removal, bc-specific all present |
| `OperatorRecord_IsStatementRemoval_WhenReplacementNull` | `Assert.True(op.IsStatementRemoval)` |
| `OperatorRecord_IsNotStatementRemoval_WhenReplacementSet` | `Assert.False(op.IsStatementRemoval)` |
| `OperatorRecord_BcSpecific_HasArgumentMatch` | identifier, argument_match, replacement all populated |
| `LoadFromPath_NonexistentFile_Throws` | `Assert.Throws<FileNotFoundException>` |
| `Validate_MissingOperatorsKey_ReturnsErrors` | errors list is non-empty |
| `Validate_InvalidCategory_ReturnsErrors` | errors list is non-empty |

**Definition of done:** All 10 tests green. `operators/default.json` loads and validates correctly from embedded resource.

### Phase 2: AL Scanner

**Implement:** `MutationCandidate` record, `AlScanner` class, `MutationVisitor` (NavSyntaxWalker subclass).

**Tests written FIRST (22 tests):**

| Test Name | Asserts |
|-----------|---------|
| `ScanFile_Sample_FindsMutations` | `Assert.NotEmpty(candidates)` |
| `ScanFile_Candidate_HasRequiredFields` | File, Line > 0, OperatorId, Original, Mutated all populated |
| `ScanFile_FindsRelationalOperators` | At least one `rel-*` operator found |
| `ScanFile_SkipsLineComments` | No candidates on comment line 10 |
| `ScanFile_SkipsBlockComments` | No candidates on lines 26-27 |
| `ScanFile_SkipsStringLiterals` | No relational/arithmetic candidates from string content |
| `ScanFile_FindsGtOnLine5` | `rel-gt-to-gte` on line 5, mutated contains `>=` |
| `ScanFile_FindsGteOnLine6` | `rel-gte-to-gt` on line 6 |
| `ScanFile_FindsNeqOnLine31` | `rel-neq-to-eq` on line 31 |
| `ScanFile_FindsAdditiveOnLine14` | `arith-add-to-sub` on line 14, mutated contains `-` |
| `ScanFile_FindsMultiplicative` | At least one `arith-mul-to-div` |
| `ScanFile_FindsLogicalAnd` | `logic-and-to-or`, mutated contains `or` |
| `ScanFile_FindsStatementRemoval` | At least one `stmt-remove-*`, mutated starts with `//` |
| `ScanFile_FindsModifyTrue` | `bc-modify-trigger-true`, mutated contains `false` |
| `ScanFile_NoMatches_ReturnsEmpty` | `Assert.Empty(candidates)` |
| `ScanFile_FindsBoolTrueToFalse` | `bool-true-to-false` found |
| `ScanFile_FindsBoolFalseToTrue` | `bool-false-to-true` found |
| `ScanFile_FindsUnaryRemoveNot` | `unary-remove-not` found, mutated lacks `not ` |
| `ScanFile_FindsExitTrueToFalse` | `exit-true-to-false` on line 50 |
| `ScanFile_FindsExitFalseToTrue` | `exit-false-to-true` on line 68 |
| `ScanFile_FindsFindSetToFindFirst` | `bc-findset-to-findfirst`, mutated contains `FindFirst` |
| `ScanDirectory_FindsOnlyAlFiles` | All candidates have `.al` file extension |

**Definition of done:** All 22 tests green. Scanner correctly identifies all 33 operators from `default.json` in fixture files.

**Note on AL Compiler API discovery:** The exact `NavSyntaxTree` API may need exploration. If the binary expression node types differ from expected names, adjust the visitor. The fixture tests will catch any mapping errors immediately.

### Phase 3: Mutator

**Implement:** `Mutator` class, `MutationException` type.

**Tests written FIRST (5 tests):**

| Test Name | Asserts |
|-----------|---------|
| `Apply_ReplacesTargetLine` | File content has mutated line, not original |
| `Apply_OnlyChangesTargetLine` | All other lines unchanged |
| `Apply_StatementRemoval_CommentsOutLine` | Line starts with `//` |
| `Apply_OriginalMismatch_ThrowsMutationException` | `Assert.Throws<MutationException>` |
| `Apply_LineOutOfRange_ThrowsMutationException` | `Assert.Throws<MutationException>` |

**Definition of done:** All 5 tests green. Uses temp files created in test setup.

### Phase 4: Git Service

**Implement:** `GitService` class.

**Tests written FIRST (4 tests):**

| Test Name | Asserts |
|-----------|---------|
| `IsWorkingTreeClean_CleanRepo_ReturnsTrue` | `Assert.True` |
| `IsWorkingTreeClean_DirtyRepo_ReturnsFalse` | `Assert.False` |
| `RestoreFile_AfterModification_RestoresContent` | File matches original |
| `RestoreFile_NoGitRepo_ThrowsMutationException` | `Assert.Throws<MutationException>` |

These tests create temporary git repos using `git init` / `git add` / `git commit` in temp directories.

**Definition of done:** All 4 tests green.

### Phase 5: Mutation Log

**Implement:** `MutationLog` class, `MutationLogFile` / `MutationRun` records.

**Tests written FIRST (8 tests):**

| Test Name | Asserts |
|-----------|---------|
| `Create_EmptyLog_HasNoRuns` | `Assert.Empty(log.Runs)` |
| `Load_ExistingLog_ParsesCorrectly` | Run count, status values match |
| `AppendRun_IncrementsRunNumber` | Run 1, then Run 2 |
| `Save_ThenReload_RoundTrips` | Saved data matches reloaded data |
| `GetSurvivedFromLastRun_FiltersSurvived` | Only SURVIVED results returned |
| `GetSurvivedFromLastRun_EmptyLog_ReturnsEmpty` | `Assert.Empty` |
| `NextMutationId_EmptyLog_ReturnsM001` | `Assert.Equal("M001", ...)` |
| `NextMutationId_AfterAppend_Increments` | `Assert.Equal("M002", ...)` |

**Definition of done:** All 8 tests green. JSON format matches existing `mutations.json` schema exactly.

### Phase 6: Test Runner (AL Runner Integration)

**Implement:** `ITestRunner` interface, `AlRunnerTestRunner` class, `TestRunner` tests with mock.

**Tests written FIRST (6 tests):**

| Test Name | Asserts |
|-----------|---------|
| `RunTests_AllPass_ReturnsTrue` | `Assert.True(result.Passed)` |
| `RunTests_SomeFail_ReturnsFalse` | `Assert.False(result.Passed)` |
| `RunTests_SomeFail_ReturnsFailedTestNames` | `Assert.Contains("TestFoo", result.FailedTests)` |
| `RunTests_CompileError_ReturnsFalse` | `Assert.False(result.Passed)` |
| `RunBaseline_Passes_ReturnsTrue` | `Assert.True(result.Passed)` |
| `RunBaseline_Fails_ReturnsFalse` | `Assert.False(result.Passed)` |

These tests use a mock `ITestRunner`. Integration tests with real `AlRunnerPipeline` come in Phase 8.

**Definition of done:** All 6 tests green with mocked runner.

### Phase 7: Report Generator

**Implement:** `ReportGenerator` class.

**Tests written FIRST (6 tests):**

| Test Name | Asserts |
|-----------|---------|
| `CalculateScore_MixedResults_CorrectPercentage` | `Assert.Equal(66.67, score, 2)` |
| `CalculateScore_AllKilled_Returns100` | `Assert.Equal(100.0, score)` |
| `CalculateScore_NoneKilled_Returns0` | `Assert.Equal(0.0, score)` |
| `CalculateScore_EmptyResults_Returns100` | `Assert.Equal(100.0, score)` |
| `GenerateMarkdown_ContainsScore` | `Assert.Contains("66.67%", md)` |
| `GenerateMarkdown_ListsSurvivors` | `Assert.Contains("M002", md)` |

**Definition of done:** All 6 tests green.

### Phase 8: Pipeline + CLI

**Implement:** `MutationPipeline`, `Program.cs`.

**Tests written FIRST (12 tests):**

Pipeline in-process tests (mocked runner):

| Test Name | Asserts |
|-----------|---------|
| `Scan_ReturnsCorrectCandidateCount` | Count matches scanner output |
| `Scan_NoCandidates_ReturnsEmpty` | `Assert.Empty(result.Results)` |
| `Run_KilledMutation_StatusIsKilled` | Result status is Killed |
| `Run_SurvivedMutation_StatusIsSurvived` | Result status is Survived |
| `Run_CompileError_StatusIsCompileError` | Result status is CompileError |
| `Run_MaxMutations_LimitsCandidates` | Result count <= max |
| `Replay_ReplaysSurvivedOnly` | Only survived mutations re-tested |
| `Run_DirtyWorkingTree_ReturnsError` | ExitCode != 0, error message |

CLI out-of-process tests (via `CliRunner`):

| Test Name | Asserts |
|-----------|---------|
| `Cli_Scan_PrintsCandidates` | StdOut contains operator IDs |
| `Cli_NoArgs_PrintsHelp` | StdErr contains usage info |
| `Cli_Scan_WithCustomOperators_Works` | ExitCode 0 |
| `Cli_InvalidCommand_ReturnsError` | ExitCode != 0 |

**Definition of done:** All 12 tests green. CLI parses all three subcommands correctly.

### Phase 9: End-to-End Integration Tests

**Implement:** Real fixture tests that scan + mutate + run AL Runner.

**Tests written FIRST (3 tests):**

| Test Name | Asserts |
|-----------|---------|
| `EndToEnd_Relational_DetectsMutations` | Scan finds relational candidates in fixture AL |
| `EndToEnd_FullRun_ProducesLog` | mutations.json written, contains results |
| `EndToEnd_MutationKilled_ByFailingTest` | At least one KILLED mutation in results |

These require fixture AL projects with both source and test code that AL Runner can execute. The fixture structure follows AL Runner's `tests/NN-name/src/` + `tests/NN-name/test/` pattern.

**Definition of done:** All 3 tests green against real AL Runner pipeline.

### Total Test Count: ~76 tests across 9 phases

---

## 5. Test Fixture Design

### Directory Structure

```
AlMutate.Tests/
  fixtures/
    sample.al          # Reuse from Python project (all operator categories)
    NoMatches.al       # Empty codeunit (no mutable code)
tests/
  01-relational/
    src/
      CreditManagement.al    # codeunit with >, >=, <, <=, =, <> in procedure bodies
    test/
      CreditManagementTest.al  # test codeunit exercising boundary conditions
  02-arithmetic/
    src/
      Calculator.al           # codeunit with +, -, *, /
    test/
      CalculatorTest.al       # tests that verify arithmetic results
  03-statement-removal/
    src/
      RecordProcessor.al      # codeunit with Modify, Insert, Delete, Validate, Error calls
    test/
      RecordProcessorTest.al  # tests that detect missing DML calls
```

### Fixture AL File Requirements

`sample.al` (already exists, reuse as-is):
- Contains all operator categories: relational (>, >=, <, <>, =), arithmetic (+, *), logical (and), boolean (true, false), unary (not), control-flow (exit(true), exit(false)), statement-removal (Modify, Insert, Error, TestField, SetRange, SetFilter, Commit, Init, DeleteAll), bc-specific (Modify(true), DeleteAll(true), FindSet)
- Contains comments (line and block) that must NOT produce mutations
- Contains an empty procedure (NoMatches.al) that produces zero candidates

End-to-end fixture files must be valid AL that AL Runner can transpile and execute. Each fixture:
- Has a `codeunit NN "Name"` with procedures containing mutable code
- Has a `codeunit NN "Name Test"` with `Subtype = Test` and `[Test]` procedures
- Tests are designed so that specific mutations cause test failures (for KILLED verification)

### CliRunner Pattern

Following AL Runner's `CliRunner.cs`:

```csharp
public static class CliRunner
{
    private static readonly string RepoRoot = Path.GetFullPath(
        Path.Combine(AppContext.BaseDirectory, "..", "..", "..", ".."));
    private static readonly string ProjectPath = Path.Combine(RepoRoot, "AlMutate");

    public record CliResult(int ExitCode, string StdOut, string StdErr);

    public static async Task<CliResult> RunAsync(string args, int timeoutMs = 120_000)
    {
        var psi = new ProcessStartInfo
        {
            FileName = "dotnet",
            Arguments = $"run --no-build --project \"{ProjectPath}\" -- {args}",
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true,
            WorkingDirectory = RepoRoot
        };
        // ... same pattern as AL Runner
    }
}
```

---

## 6. AL Runner Integration

### Consumption Model: Project Reference (Monorepo)

AL Runner lives at `/home/stefan/Documents/Repos/community/BusinessCentral.AL.Runner/`. AlMutate references it as a project reference:

```xml
<ProjectReference Include="../../BusinessCentral.AL.Runner/AlRunner/AlRunner.csproj" />
```

This gives direct access to `AlRunnerPipeline`, `PipelineOptions`, `PipelineResult`, and `TestResult`. No subprocess spawning, no JSON protocol, no server mode.

If the projects are later separated into distinct repos, switch the project reference to a NuGet package reference (`MSDyn365BC.AL.Runner`). The API surface stays identical.

### API Usage

```csharp
internal class AlRunnerTestRunner : ITestRunner
{
    private readonly AlRunnerPipeline _pipeline = new();

    public TestRunResult RunTests(string sourcePath, string testPath)
    {
        var result = _pipeline.Run(new PipelineOptions
        {
            InputPaths = { sourcePath, testPath }
        });

        var failedTests = result.Tests
            .Where(t => t.Status != TestStatus.Pass)
            .Select(t => t.Name)
            .ToList();

        return new TestRunResult(
            Passed: result.ExitCode == 0,
            FailedTests: failedTests,
            CompileError: result.StdErr.Contains("Error:") && result.Tests.Count == 0
        );
    }
}
```

The key insight: AL Runner does the full AL-to-C#-to-execution pipeline in-process. Each mutation cycle becomes:

1. `Mutator.Apply(candidate)` -- modify .al file on disk
2. `_pipeline.Run(options)` -- AL Runner re-reads the file, transpiles, compiles, runs tests
3. Check `result.ExitCode` and `result.Tests` for pass/fail
4. `GitService.RestoreFile(candidate.File)` -- restore via git

No `al-compile`, no `bc-publish`, no `run-tests.sh`. AL Runner replaces the entire BC Linux stack for test execution.

### Interface for Testability

```csharp
public interface ITestRunner
{
    TestRunResult RunTests(string sourcePath, string testPath);
}

public record TestRunResult(bool Passed, List<string> FailedTests, bool CompileError);
```

Unit tests inject a mock `ITestRunner`. Integration tests use the real `AlRunnerTestRunner`.

### Handling AL Runner Unavailability

AL Runner is a hard dependency. If the AL compiler DLLs are missing, AL Runner's MSBuild targets auto-download them. If download fails, the build fails -- this is the correct behavior. There is no graceful degradation; mutation testing without a test runner is meaningless.

---

## 7. Operator JSON

### Format: Reuse As-Is

The existing `operators/default.json` and `operators/schema.json` are reused unchanged. The 33 operators across 8 categories remain identical.

### C# Model Classes

```csharp
public record MutationOperator
{
    [JsonPropertyName("id")]
    public required string Id { get; init; }

    [JsonPropertyName("name")]
    public required string Name { get; init; }

    [JsonPropertyName("category")]
    public required string Category { get; init; }

    [JsonPropertyName("node_type")]
    public required string NodeType { get; init; }

    [JsonPropertyName("operator_token")]
    public string? OperatorToken { get; init; }

    [JsonPropertyName("identifier")]
    public string? Identifier { get; init; }

    [JsonPropertyName("argument_match")]
    public string? ArgumentMatch { get; init; }

    [JsonPropertyName("identifier_replacement")]
    public string? IdentifierReplacement { get; init; }

    [JsonPropertyName("replacement")]
    public string? Replacement { get; init; }

    [JsonIgnore]
    public bool IsStatementRemoval => Replacement is null && IdentifierReplacement is null;
}

public record OperatorFile
{
    [JsonPropertyName("operators")]
    public required MutationOperator[] Operators { get; init; }
}
```

### Schema Validation

No external JSON Schema library. Validation is done in code:

```csharp
internal static List<string> Validate(OperatorFile file)
{
    var errors = new List<string>();
    if (file.Operators.Length == 0)
        errors.Add("operators array must not be empty");

    var validCategories = new HashSet<string>
    {
        "relational", "arithmetic", "logical", "boolean",
        "statement-removal", "boundary", "control-flow", "bc-specific"
    };

    var ids = new HashSet<string>();
    foreach (var op in file.Operators)
    {
        if (string.IsNullOrEmpty(op.Id)) errors.Add("operator missing id");
        if (string.IsNullOrEmpty(op.Name)) errors.Add($"operator {op.Id} missing name");
        if (!validCategories.Contains(op.Category))
            errors.Add($"operator {op.Id} has invalid category '{op.Category}'");
        if (!ids.Add(op.Id))
            errors.Add($"duplicate operator id '{op.Id}'");
    }
    return errors;
}
```

This is simpler and faster than loading the JSON Schema spec, and catches the same errors. The JSON Schema file (`schema.json`) remains in the repo for documentation and for other tools that want to validate operator files.

---

## 8. Cleanup / Files to Delete

### Python Files to Remove

```
al_mutate/                    # Entire directory
  __init__.py
  cli.py
  log.py
  mutate.py
  operators.py
  report.py
  run.py
  scan.py
al_mutate.egg-info/           # Entire directory
tests/                        # Entire directory (replaced by AlMutate.Tests/)
  __init__.py
  test_cli.py
  test_log.py
  test_mutate.py
  test_operators.py
  test_report.py
  test_run.py
  test_scan.py
  fixtures/                   # Move sample.al + NoMatches.al to AlMutate.Tests/fixtures/ first
pyproject.toml
.venv/                        # Virtual environment
```

### Files to Keep

```
operators/default.json        # Copied into AlMutate/operators/ as embedded resource
operators/schema.json         # Copied into AlMutate/operators/ as embedded resource
LICENSE
README.md                    # Update to reflect C# tool
docs/ARCHITECTURE.md          # Update
docs/OPERATORS.md             # Keep (operator format unchanged)
docs/USAGE.md                 # Update CLI examples
CLAUDE.md                     # Rewrite for C# project
```

### Old PowerShell Files (Already Obsolete)

```
BCMutations/                  # Delete if still present
entrypoint.ps1                # Delete if still present
action.yml                    # Delete if still present
tests/Unit/                   # Delete if still present
tests/Integration/            # Delete if still present
```

### CLAUDE.md Updates Needed

Rewrite `CLAUDE.md` to reflect:
- C# / .NET 8 / dotnet tool instead of Python CLI
- AL Runner integration instead of BC Linux stack
- xUnit instead of pytest
- `NavSyntaxTree` instead of tree-sitter-al
- Solution structure with AlMutate.slnx
- Remove all references to Python, pip, tree-sitter, pyproject.toml
- Update bootstrap order to match TDD phases
- Update execution flow to use AL Runner pipeline instead of subprocess calls

---

## Summary: Implementation Order

| Phase | What | Tests First | Estimated Tests |
|-------|------|-------------|-----------------|
| 0 | Create solution, csproj files, empty classes | - | 0 |
| 1 | Operator loading + validation | 10 | 10 |
| 2 | AL Scanner (NavSyntaxTree walker) | 22 | 22 |
| 3 | Mutator (apply line replacement) | 5 | 5 |
| 4 | Git service (clean check + restore) | 4 | 4 |
| 5 | Mutation log (mutations.json) | 8 | 8 |
| 6 | Test runner (AL Runner integration) | 6 | 6 |
| 7 | Report generator (Markdown) | 6 | 6 |
| 8 | Pipeline + CLI | 12 | 12 |
| 9 | End-to-end integration | 3 | 3 |
| **Total** | | | **~76** |

Each phase is self-contained: write the failing tests, implement until green, commit, move to next phase. No phase depends on a later phase. The only cross-phase dependency is that Phase 2 (Scanner) uses Phase 1 (Operators), Phase 8 (Pipeline) uses everything, and Phase 9 (E2E) validates the full stack.
