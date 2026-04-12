using AlMutate;
using AlMutate.Models;
using AlMutate.Services;
using Xunit;

namespace AlMutate.Tests;

/// <summary>
/// In-process pipeline tests using FakeTestRunner.
/// These do NOT require a running BC instance.
/// </summary>
public class PipelineTests
{
    private static readonly string FixturesDir = Path.Combine(
        AppContext.BaseDirectory, "fixtures");

    private static readonly string SampleDir = FixturesDir;
    private static readonly string NoMatchesDir = FixturesDir;

    // Helper: build a pipeline with a FakeTestRunner
    private static MutationPipeline MakePipeline(bool baselinePasses = true, bool mutantPasses = true, bool compileError = false)
        => new MutationPipeline(
            new FakeTestRunner(baselinePasses, compileError: compileError),
            operatorsPath: null);

    // -----------------------------------------------------------------------
    // Scan_ReturnsCorrectCandidateCount
    // -----------------------------------------------------------------------
    [Fact]
    public void Scan_ReturnsCorrectCandidateCount()
    {
        var pipeline = MakePipeline();

        var result = pipeline.Scan(new PipelineOptions { SourcePath = SampleDir });

        Assert.True(result.Results.Count > 0, "Expected at least one mutation candidate from sample.al");
        Assert.Equal(0, result.ExitCode);
    }

    // -----------------------------------------------------------------------
    // Scan_NoCandidates_ReturnsEmpty
    // -----------------------------------------------------------------------
    [Fact]
    public void Scan_NoCandidates_ReturnsEmpty()
    {
        // Use a temp dir with only NoMatches.al (no mutable code)
        var tempDir = Path.Combine(Path.GetTempPath(), $"almutate_test_{Guid.NewGuid():N}");
        Directory.CreateDirectory(tempDir);
        try
        {
            File.Copy(Path.Combine(NoMatchesDir, "NoMatches.al"), Path.Combine(tempDir, "NoMatches.al"));
            var pipeline = MakePipeline();

            var result = pipeline.Scan(new PipelineOptions { SourcePath = tempDir });

            Assert.Empty(result.Results);
            Assert.Equal(0, result.ExitCode);
        }
        finally
        {
            Directory.Delete(tempDir, true);
        }
    }

    // -----------------------------------------------------------------------
    // Run_KilledMutation_StatusIsKilled
    // -----------------------------------------------------------------------
    [Fact]
    public void Run_KilledMutation_StatusIsKilled()
    {
        // Fake runner: baseline passes, mutations fail → Killed
        var pipeline = new MutationPipeline(
            new SequencedFakeRunner(
                baseline: new TestRunResult(true, new List<string>()),
                mutation: new TestRunResult(false, new List<string> { "SomeTest" })),
            operatorsPath: null);

        var tempDir = CreateTempGitRepo(FixturesDir);
        try
        {
            var result = pipeline.Run(new PipelineOptions
            {
                SourcePath = tempDir,
                TestPath = "/dev/null",
                MaxMutations = 1,
                LogFilePath = Path.Combine(tempDir, "mutations.json")
            });

            Assert.Equal(0, result.ExitCode);
            Assert.Contains(result.Results, r => r.Status == MutationStatus.Killed);
        }
        finally
        {
            Directory.Delete(tempDir, true);
        }
    }

    // -----------------------------------------------------------------------
    // Run_SurvivedMutation_StatusIsSurvived
    // -----------------------------------------------------------------------
    [Fact]
    public void Run_SurvivedMutation_StatusIsSurvived()
    {
        // Fake runner: both baseline and mutation pass → Survived
        var pipeline = new MutationPipeline(
            new FakeTestRunner(passed: true),
            operatorsPath: null);

        var tempDir = CreateTempGitRepo(FixturesDir);
        try
        {
            var result = pipeline.Run(new PipelineOptions
            {
                SourcePath = tempDir,
                TestPath = "/dev/null",
                MaxMutations = 1,
                LogFilePath = Path.Combine(tempDir, "mutations.json")
            });

            Assert.Equal(0, result.ExitCode);
            Assert.Contains(result.Results, r => r.Status == MutationStatus.Survived);
        }
        finally
        {
            Directory.Delete(tempDir, true);
        }
    }

    // -----------------------------------------------------------------------
    // Run_CompileError_StatusIsCompileError
    // -----------------------------------------------------------------------
    [Fact]
    public void Run_CompileError_StatusIsCompileError()
    {
        // Fake runner: baseline passes, mutation results in compile error
        var pipeline = new MutationPipeline(
            new SequencedFakeRunner(
                baseline: new TestRunResult(true, new List<string>()),
                mutation: new TestRunResult(false, new List<string>(), CompileError: true)),
            operatorsPath: null);

        var tempDir = CreateTempGitRepo(FixturesDir);
        try
        {
            var result = pipeline.Run(new PipelineOptions
            {
                SourcePath = tempDir,
                TestPath = "/dev/null",
                MaxMutations = 1,
                LogFilePath = Path.Combine(tempDir, "mutations.json")
            });

            Assert.Equal(0, result.ExitCode);
            Assert.Contains(result.Results, r => r.Status == MutationStatus.CompileError);
        }
        finally
        {
            Directory.Delete(tempDir, true);
        }
    }

    // -----------------------------------------------------------------------
    // Run_MaxMutations_LimitsCandidates
    // -----------------------------------------------------------------------
    [Fact]
    public void Run_MaxMutations_LimitsCandidates()
    {
        var pipeline = new MutationPipeline(
            new FakeTestRunner(passed: true),
            operatorsPath: null);

        var tempDir = CreateTempGitRepo(FixturesDir);
        try
        {
            var result = pipeline.Run(new PipelineOptions
            {
                SourcePath = tempDir,
                TestPath = "/dev/null",
                MaxMutations = 1,
                LogFilePath = Path.Combine(tempDir, "mutations.json")
            });

            Assert.Equal(0, result.ExitCode);
            Assert.Equal(1, result.Results.Count);
        }
        finally
        {
            Directory.Delete(tempDir, true);
        }
    }

    // -----------------------------------------------------------------------
    // Run_DirtyWorkingTree_ReturnsError
    // -----------------------------------------------------------------------
    [Fact]
    public void Run_DirtyWorkingTree_ReturnsError()
    {
        var pipeline = MakePipeline();
        var tempDir = CreateTempGitRepo(FixturesDir);

        // Make the working tree dirty by modifying a tracked file
        var files = Directory.GetFiles(tempDir, "*.al");
        File.AppendAllText(files[0], "\n// dirty");

        try
        {
            var result = pipeline.Run(new PipelineOptions
            {
                SourcePath = tempDir,
                TestPath = "/dev/null",
                LogFilePath = Path.Combine(tempDir, "mutations.json")
            });

            Assert.Equal(1, result.ExitCode);
            Assert.NotNull(result.ErrorMessage);
            Assert.Contains("dirty", result.ErrorMessage, StringComparison.OrdinalIgnoreCase);
        }
        finally
        {
            Directory.Delete(tempDir, true);
        }
    }

    // -----------------------------------------------------------------------
    // Run_RunnerThrowsNonMutationException_MarksAsCompileError_AndContinues
    // A runner throwing IOException (e.g. broken pipe to a crashed server)
    // must NOT crash the pipeline — it should mark the mutation as CompileError
    // and continue to the next candidate.
    // -----------------------------------------------------------------------
    [Fact]
    public void Run_RunnerThrowsNonMutationException_MarksAsCompileError_AndContinues()
    {
        // Baseline passes; first mutation throws IOException; second mutation is killed.
        var pipeline = new MutationPipeline(
            new SequencedFakeRunner(
                baseline: new TestRunResult(true, []),
                mutation: new TestRunResult(false, ["SomeTest"])),
            operatorsPath: null);

        // Use a runner that throws on the first mutation call
        var throwingPipeline = new MutationPipeline(
            new ThrowingAfterBaselineRunner(),
            operatorsPath: null);

        var tempDir = CreateTempGitRepo(FixturesDir);
        try
        {
            var result = throwingPipeline.Run(new PipelineOptions
            {
                SourcePath = tempDir,
                TestPath = "/dev/null",
                MaxMutations = 2,
                LogFilePath = Path.Combine(tempDir, "mutations.json"),
            });

            Assert.Equal(0, result.ExitCode);
            // The throwing mutation should be CompileError, not crash
            Assert.Contains(result.Results, r => r.Status == MutationStatus.CompileError);
            // Both candidates should be recorded
            Assert.Equal(2, result.Results.Count);
        }
        finally
        {
            Directory.Delete(tempDir, true);
        }
    }

    // -----------------------------------------------------------------------
    // Run_MutationTimeoutExceeded_MarksAsTimedOut_AndContinues
    // -----------------------------------------------------------------------
    [Fact]
    public void Run_MutationTimeoutExceeded_MarksAsTimedOut_AndContinues()
    {
        // Baseline is instant; first mutation takes 500ms (exceeds 50ms timeout);
        // second mutation returns instantly. Both should be recorded and the run
        // should complete rather than hang.
        var pipeline = new MutationPipeline(
            new TimingFakeRunner(
                baselineDelay: TimeSpan.Zero,
                mutationDelay: TimeSpan.FromMilliseconds(500)),
            operatorsPath: null);

        var tempDir = CreateTempGitRepo(FixturesDir);
        try
        {
            var result = pipeline.Run(new PipelineOptions
            {
                SourcePath = tempDir,
                TestPath = "/dev/null",
                MaxMutations = 2,
                LogFilePath = Path.Combine(tempDir, "mutations.json"),
                MutationTimeout = TimeSpan.FromMilliseconds(50),
            });

            Assert.Equal(0, result.ExitCode);
            // At least one mutation should be TimedOut
            Assert.Contains(result.Results, r => r.Status == MutationStatus.TimedOut);
            // Run completed — all MaxMutations candidates should be recorded
            Assert.Equal(2, result.Results.Count);
        }
        finally
        {
            Directory.Delete(tempDir, true);
        }
    }

    // -----------------------------------------------------------------------
    // Replay_NotImplemented_Throws
    // -----------------------------------------------------------------------
    [Fact]
    public void Replay_NotImplemented_Throws()
    {
        var pipeline = MakePipeline();

        Assert.Throws<NotImplementedException>(() => pipeline.Replay(new PipelineOptions
        {
            LogFilePath = "mutations.json",
            TestPath = "/dev/null"
        }));
    }

    // -----------------------------------------------------------------------
    // Helpers
    // -----------------------------------------------------------------------

    /// <summary>
    /// Creates a temp directory with a git repo, copies fixture .al files into it,
    /// and commits them so the working tree is clean.
    /// </summary>
    private static string CreateTempGitRepo(string sourceDir)
    {
        var tempDir = Path.Combine(Path.GetTempPath(), $"almutate_test_{Guid.NewGuid():N}");
        Directory.CreateDirectory(tempDir);

        // Copy .al fixture files
        foreach (var f in Directory.GetFiles(sourceDir, "*.al", SearchOption.TopDirectoryOnly))
        {
            File.Copy(f, Path.Combine(tempDir, Path.GetFileName(f)));
        }

        // Init git repo and commit
        RunCommand("git", "init", tempDir);
        RunCommand("git", "config user.email test@test.com", tempDir);
        RunCommand("git", "config user.name Test", tempDir);
        RunCommand("git", "add .", tempDir);
        RunCommand("git", "commit -m \"initial\"", tempDir);

        return tempDir;
    }

    private static void RunCommand(string cmd, string args, string workDir)
    {
        var psi = new System.Diagnostics.ProcessStartInfo(cmd, args)
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

/// <summary>
/// A test runner that returns different results for baseline vs mutations.
/// Call 1 = baseline, subsequent calls = mutation runs.
/// </summary>
internal class SequencedFakeRunner : ITestRunner
{
    private readonly TestRunResult _baseline;
    private readonly TestRunResult _mutation;
    private int _callCount;

    public SequencedFakeRunner(TestRunResult baseline, TestRunResult mutation)
    {
        _baseline = baseline;
        _mutation = mutation;
    }

    public TestRunResult RunTests(string sourcePath, string testPath, string? stubsPath = null)
    {
        _callCount++;
        return _callCount == 1 ? _baseline : _mutation;
    }
}

/// <summary>
/// A test runner whose baseline passes but every mutation call throws IOException
/// (simulates a crashed al-runner server with a broken pipe).
/// </summary>
internal class ThrowingAfterBaselineRunner : ITestRunner
{
    private int _callCount;

    public TestRunResult RunTests(string sourcePath, string testPath, string? stubsPath = null)
    {
        _callCount++;
        if (_callCount == 1)
            return new TestRunResult(true, new List<string>());
        throw new System.IO.IOException("Simulated broken pipe");
    }
}

/// <summary>
/// A test runner that sleeps for a configurable delay to simulate slow test runs.
/// Baseline (call 1) uses <paramref name="baselineDelay"/>; mutations use <paramref name="mutationDelay"/>.
/// </summary>
internal class TimingFakeRunner : ITestRunner
{
    private readonly TimeSpan _baselineDelay;
    private readonly TimeSpan _mutationDelay;
    private int _callCount;

    public TimingFakeRunner(TimeSpan baselineDelay, TimeSpan mutationDelay)
    {
        _baselineDelay = baselineDelay;
        _mutationDelay = mutationDelay;
    }

    public TestRunResult RunTests(string sourcePath, string testPath, string? stubsPath = null)
    {
        _callCount++;
        var delay = _callCount == 1 ? _baselineDelay : _mutationDelay;
        if (delay > TimeSpan.Zero)
            Thread.Sleep(delay);
        return new TestRunResult(true, new List<string>());
    }
}
