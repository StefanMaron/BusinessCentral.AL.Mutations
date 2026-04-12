using AlMutate;
using AlMutate.Models;
using AlMutate.Services;
using Xunit;

namespace AlMutate.Tests;

/// <summary>
/// End-to-end integration tests using the real AlRunnerTestRunner against the
/// 01-credit fixture. These tests do not mock any part of the pipeline.
///
/// Run them in isolation with:
///   dotnet test --filter Category=Integration
/// Exclude them in CI with:
///   dotnet test --filter Category!=Integration
/// </summary>
[Collection("Integration")]
public class IntegrationTests
{
    // -----------------------------------------------------------------------
    // Paths to the 01-credit fixture
    // -----------------------------------------------------------------------

    /// <summary>
    /// Repo root: AppContext.BaseDirectory is
    ///   AlMutate/AlMutate.Tests/bin/Debug/net8.0/
    /// so going up 4 levels lands at the AlMutate/ solution folder, then one
    /// more level to the repo root that contains the AlMutate/ folder.
    /// </summary>
    private static readonly string RepoRoot = Path.GetFullPath(
        Path.Combine(AppContext.BaseDirectory, "..", "..", "..", ".."));

    private static readonly string FixtureSrc = Path.Combine(RepoRoot, "tests", "01-credit", "src");
    private static readonly string FixtureTest = Path.Combine(RepoRoot, "tests", "01-credit", "test");

    // -----------------------------------------------------------------------
    // Integration_AlRunner_BaselinePassesClean
    // -----------------------------------------------------------------------
    [Fact]
    [Trait("Category", "Integration")]
    public void Integration_AlRunner_BaselinePassesClean()
    {
        // Arrange
        var runner = new AlRunnerTestRunner();

        // Act
        var result = runner.RunTests(FixtureSrc, FixtureTest);

        // Assert
        Assert.True(result.Passed,
            $"Baseline tests must all pass on unmodified fixture. " +
            $"CompileError={result.CompileError}, " +
            $"FailedTests=[{string.Join(", ", result.FailedTests)}]");
        Assert.False(result.CompileError, "Baseline should not produce a compile error");
        Assert.Empty(result.FailedTests);
    }

    // -----------------------------------------------------------------------
    // Integration_Scanner_FindsCandidatesInFixture
    // -----------------------------------------------------------------------
    [Fact]
    [Trait("Category", "Integration")]
    public void Integration_Scanner_FindsCandidatesInFixture()
    {
        // Arrange
        var operators = new OperatorLoader().Load();

        // Act
        var candidates = AlScanner.ScanDirectory(FixtureSrc, operators);

        // Assert
        Assert.NotEmpty(candidates);

        // The fixture contains 'if Amount > CreditLimit' — must find rel-gt-to-gte
        var relGt = candidates.FirstOrDefault(c => c.OperatorId == "rel-gt-to-gte");
        Assert.NotNull(relGt);
        Assert.Contains(">=", relGt.Mutated);

        // Must also find arithmetic operators from ApplyDiscount
        var arith = candidates.Where(c => c.OperatorId.StartsWith("arith-")).ToList();
        Assert.NotEmpty(arith);
    }

    // -----------------------------------------------------------------------
    // Integration_FullPipeline_ProducesResults
    // -----------------------------------------------------------------------
    [Fact]
    [Trait("Category", "Integration")]
    public void Integration_FullPipeline_ProducesResults()
    {
        // The pipeline mutates files in-place and restores via git, so the
        // fixture must live inside a clean git working tree. Copy fixture
        // files to a temp git repo for this test.
        var tempDir = CreateTempGitRepoFromFixture();
        try
        {
            var srcDir = Path.Combine(tempDir, "src");
            var logPath = Path.Combine(tempDir, "mutations.json");

            var runner = new AlRunnerTestRunner();
            var pipeline = new MutationPipeline(runner);

            // Act: run the full pipeline
            var result = pipeline.Run(new PipelineOptions
            {
                SourcePath = srcDir,
                TestPath = FixtureTest,  // test files are never mutated — safe to use originals
                LogFilePath = logPath,
            });

            // Assert: pipeline itself must succeed
            Assert.Equal(0, result.ExitCode);
            Assert.Null(result.ErrorMessage);
            Assert.NotEmpty(result.Results);

            // The rel-gt-to-gte mutation on 'if Amount > CreditLimit' must
            // be KILLED because TestIsOverLimit_ExactLimit_ReturnsFalse tests
            // that IsOverLimit(1000,1000) == false, which fails when > becomes >=.
            var relGtMutation = result.Results
                .FirstOrDefault(r => r.Operator == "rel-gt-to-gte");
            Assert.NotNull(relGtMutation);
            Assert.Equal(MutationStatus.Killed, relGtMutation.Status);

            // At least one mutation must be KILLED overall
            Assert.Contains(result.Results, r => r.Status == MutationStatus.Killed);

            // Score must reflect that kills happened
            Assert.True(result.Score > 0.0,
                $"Expected score > 0 but was {result.Score}. Results: " +
                string.Join(", ", result.Results.Select(r => $"{r.Operator}:{r.Status}")));
        }
        finally
        {
            Directory.Delete(tempDir, recursive: true);
        }
    }

    // -----------------------------------------------------------------------
    // Helpers
    // -----------------------------------------------------------------------

    /// <summary>
    /// Creates a temp directory with a fresh git repo and copies the
    /// 01-credit fixture's src/ files into a src/ subdirectory.
    /// Returns the path to the temp directory (which contains src/).
    /// </summary>
    private static string CreateTempGitRepoFromFixture()
    {
        var tempDir = Path.Combine(
            Path.GetTempPath(),
            $"almutate_integration_{Guid.NewGuid():N}");

        var srcDir = Path.Combine(tempDir, "src");
        Directory.CreateDirectory(srcDir);

        // Copy only the src .al files (test files stay at their original location)
        foreach (var f in Directory.GetFiles(FixtureSrc, "*.al", SearchOption.TopDirectoryOnly))
        {
            File.Copy(f, Path.Combine(srcDir, Path.GetFileName(f)));
        }

        // Initialise a git repo and commit so the working tree is clean
        RunGit("init", tempDir);
        RunGit("config user.email integration@test.local", tempDir);
        RunGit("config user.name Integration", tempDir);
        RunGit("add .", tempDir);
        RunGit("commit -m \"fixture\"", tempDir);

        return tempDir;
    }

    private static void RunGit(string args, string workDir)
    {
        var psi = new System.Diagnostics.ProcessStartInfo("git", args)
        {
            WorkingDirectory = workDir,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false
        };
        using var proc = System.Diagnostics.Process.Start(psi)!;
        proc.WaitForExit();
    }
}
