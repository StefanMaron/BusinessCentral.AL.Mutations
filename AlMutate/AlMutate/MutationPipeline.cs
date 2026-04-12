using AlMutate.Models;
using AlMutate.Services;

namespace AlMutate;

public class MutationPipeline
{
    private readonly ITestRunner _runner;
    private readonly List<MutationOperator> _operators;

    public MutationPipeline(ITestRunner runner, string? operatorsPath = null)
    {
        _runner = runner;
        _operators = new OperatorLoader().Load(operatorsPath);
    }

    /// <summary>
    /// Scan source directory for mutation candidates, no test execution.
    /// </summary>
    public PipelineResult Scan(PipelineOptions options)
    {
        var candidates = AlScanner.ScanDirectory(options.SourcePath, _operators);

        var results = candidates.Select((c, i) => new MutationResult(
            $"S{i + 1:D3}",
            c.OperatorId,
            c.File,
            c.Line,
            c.Original,
            c.Mutated,
            MutationStatus.Survived,
            null)).ToList();

        return new PipelineResult(0, results, 0.0, null);
    }

    /// <summary>
    /// Full mutation testing run: check git clean, baseline, mutate, report.
    /// Uses al-runner server mode for incremental compilation when the runner
    /// is <see cref="AlRunnerTestRunner"/>, keeping compiled assemblies warm
    /// between mutations so only changed files re-transpile.
    /// </summary>
    public PipelineResult Run(PipelineOptions options)
        => RunAsync(options).GetAwaiter().GetResult();

    /// <summary>
    /// Async implementation of <see cref="Run"/>.
    /// </summary>
    public async Task<PipelineResult> RunAsync(PipelineOptions options)
    {
        void Log(string msg) { if (!options.Silent) Console.WriteLine(msg); }

        // 1. Check git clean (relative to the source directory)
        if (!GitService.IsWorkingTreeClean(options.SourcePath))
            return new PipelineResult(1, new List<MutationResult>(), 0.0, null, "Working tree is dirty");

        // 2. Scan candidates
        Log("Scanning for mutation candidates...");
        var candidates = AlScanner.ScanDirectory(options.SourcePath, _operators);
        if (options.MaxMutations.HasValue)
            candidates = candidates.Take(options.MaxMutations.Value).ToList();
        Log($"Found {candidates.Count} candidate(s).");

        // 3. Resolve the effective runner: use server mode when runner is AlRunnerTestRunner
        //    for incremental compilation (cache hit on unchanged files).
        //    For any other runner (e.g. FakeTestRunner in tests), use it directly.
        ITestRunner effectiveRunner = _runner;
        AlRunnerServer? server = null;

        if (_runner is AlRunnerTestRunner)
        {
            Log("Starting al-runner server...");
            var alRunnerPath = AlRunnerTestRunner.GetAlRunnerPath();
            server = await AlRunnerServer.StartAsync(alRunnerPath);
            effectiveRunner = server;
        }

        try
        {
            // 4. Run baseline
            Log("Running baseline tests...");
            var baseline = effectiveRunner.RunTests(options.SourcePath, options.TestPath!, options.StubsPath);
            if (!baseline.Passed && !baseline.CompileError)
                return new PipelineResult(1, new List<MutationResult>(), 0.0, null, "Baseline tests failed");
            Log("Baseline passed.");

            // 5. Load or create log
            var logPath = options.LogFilePath ?? "mutations.json";
            var log = File.Exists(logPath)
                ? MutationLog.Load(logPath)
                : MutationLog.Create(logPath, options.SourcePath);

            // 6. Run each mutation
            var results = new List<MutationResult>();
            int total = candidates.Count;
            int index = 0;
            foreach (var candidate in candidates)
            {
                index++;
                var id = log.NextMutationId();
                MutationStatus status;
                string? caughtBy = null;

                var shortFile = Path.GetFileName(candidate.File);
                var sw = System.Diagnostics.Stopwatch.StartNew();
                try
                {
                    Mutator.Apply(candidate);

                    TestRunResult testResult;
                    if (options.MutationTimeout.HasValue)
                    {
                        // Run the test on a background thread and race against a timeout.
                        // If it times out we restore the file, mark TIMED_OUT, and continue
                        // so the pipeline is never blocked by an infinite loop in mutated code.
                        var testTask = Task.Run(() =>
                            effectiveRunner.RunTests(options.SourcePath, options.TestPath!, options.StubsPath));
                        var timeoutTask = Task.Delay(options.MutationTimeout.Value);

                        if (await Task.WhenAny(testTask, timeoutTask) == timeoutTask)
                        {
                            // Timed out — restore file and mark neutral
                            sw.Stop();
                            try { GitService.RestoreFile(candidate.File); } catch { /* best effort */ }
                            status = MutationStatus.TimedOut;
                            var timeoutLabel = FormatElapsed(sw.Elapsed);
                            Log($"  [{index}/{total}] {id} [{candidate.OperatorId}] {shortFile}:{candidate.Line} → TIMED_OUT ({timeoutLabel})");
                            results.Add(new MutationResult(id, candidate.OperatorId, candidate.File,
                                candidate.Line, candidate.Original, candidate.Mutated, status, null));
                            continue;
                        }

                        testResult = await testTask; // already completed — no extra wait
                    }
                    else
                    {
                        testResult = effectiveRunner.RunTests(options.SourcePath, options.TestPath!, options.StubsPath);
                    }

                    GitService.RestoreFile(candidate.File);

                    if (testResult.CompileError)
                        status = MutationStatus.CompileError;
                    else if (testResult.Passed)
                        status = MutationStatus.Survived;
                    else
                    {
                        status = MutationStatus.Killed;
                        caughtBy = testResult.FailedTests.FirstOrDefault();
                    }
                }
                catch (MutationException)
                {
                    try { GitService.RestoreFile(candidate.File); } catch { /* best effort */ }
                    status = MutationStatus.CompileError;
                }
                sw.Stop();

                var statusLabel = status switch
                {
                    MutationStatus.Killed => "KILLED",
                    MutationStatus.Survived => "SURVIVED",
                    MutationStatus.CompileError => "COMPILE_ERROR",
                    MutationStatus.TimedOut => "TIMED_OUT",
                    _ => status.ToString().ToUpperInvariant()
                };
                Log($"  [{index}/{total}] {id} [{candidate.OperatorId}] {shortFile}:{candidate.Line} → {statusLabel} ({FormatElapsed(sw.Elapsed)})");

                results.Add(new MutationResult(
                    id,
                    candidate.OperatorId,
                    candidate.File,
                    candidate.Line,
                    candidate.Original,
                    candidate.Mutated,
                    status,
                    caughtBy));
            }

            // 7. Append run and save log
            log.AppendRun(results);
            log.Save();

            // 8. Generate report
            var score = ReportGenerator.CalculateScore(results);
            var report = ReportGenerator.GenerateMarkdown(results, options.SourcePath);

            return new PipelineResult(0, results, score, report);
        }
        finally
        {
            if (server != null)
                await server.DisposeAsync();
        }
    }

    /// <summary>
    /// Replay survived mutations from a previous log.
    /// </summary>
    public PipelineResult Replay(PipelineOptions options)
    {
        throw new NotImplementedException("Replay not yet implemented");
    }

    private static string FormatElapsed(TimeSpan elapsed) =>
        elapsed.TotalSeconds >= 1
            ? $"{elapsed.TotalSeconds:F1}s"
            : $"{elapsed.TotalMilliseconds:F0}ms";
}
