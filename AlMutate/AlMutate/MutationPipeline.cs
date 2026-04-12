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
        string? alRunnerPath = null;    // kept for server restart after timeout

        if (_runner is AlRunnerTestRunner)
        {
            Log("Starting al-runner server...");
            alRunnerPath = AlRunnerTestRunner.GetAlRunnerPath();
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
                            // Timed out — restore file and mark neutral.
                            // Also restart the server: the abandoned testTask is still blocking
                            // on ReadLine from the server's stdout. If we don't restart, the
                            // next mutation's ReadLine races with this abandoned one and the
                            // protocol desynchronizes, causing every subsequent mutation to
                            // time out as well.
                            sw.Stop();
                            if (server != null && alRunnerPath != null)
                                (server, effectiveRunner) = await RestartServerAsync(server, alRunnerPath, Log);
                            try { GitService.RestoreFile(candidate.File); } catch { /* best effort */ }
                            Log($"  [{index}/{total}] {id} [{candidate.OperatorId}] {shortFile}:{candidate.Line} → TIMED_OUT ({FormatElapsed(sw.Elapsed)})");
                            results.Add(new MutationResult(id, candidate.OperatorId, candidate.File,
                                candidate.Line, candidate.Original, candidate.Mutated, MutationStatus.TimedOut, null));
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
                catch (Exception ex)
                {
                    // Non-MutationException (e.g. IOException from a crashed server process).
                    // Log to stderr, mark as CompileError, restart the server if applicable,
                    // and continue — never let one bad mutation abort the whole run.
                    Console.Error.WriteLine($"Unexpected error on {id}: {ex.GetType().Name}: {ex.Message}");
                    try { GitService.RestoreFile(candidate.File); } catch { /* best effort */ }
                    status = MutationStatus.CompileError;
                    if (server != null && alRunnerPath != null)
                        (server, effectiveRunner) = await RestartServerAsync(server, alRunnerPath, Log);
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
    /// Re-runs all SURVIVED mutations from the most recent run to check whether
    /// improved tests now kill them.
    /// </summary>
    public PipelineResult Replay(PipelineOptions options)
    {
        void Log(string msg) { if (!options.Silent) Console.WriteLine(msg); }

        var logPath = options.LogFilePath ?? "mutations.json";
        var log = MutationLog.Load(logPath);

        var survived = log.GetSurvivedFromLastRun();
        Log($"Replaying {survived.Count} survived mutation(s)...");

        if (survived.Count == 0)
        {
            var emptyReport = ReportGenerator.GenerateMarkdown(new List<MutationResult>(), log.SourcePath);
            return new PipelineResult(0, new List<MutationResult>(), 1.0, emptyReport);
        }

        var sourcePath = log.SourcePath;
        var testPath = options.TestPath ?? "";
        var results = new List<MutationResult>();

        int index = 0;
        int total = survived.Count;
        foreach (var mutation in survived)
        {
            index++;
            MutationStatus status;
            string? caughtBy = null;

            // Check if the original line still exists in the file
            if (!FileContainsOriginal(mutation.File, mutation.Line, mutation.Original))
            {
                Log($"  [{index}/{total}] {mutation.Id} [{mutation.Operator}] {Path.GetFileName(mutation.File)}:{mutation.Line} → OBSOLETE");
                results.Add(mutation with { Status = MutationStatus.Obsolete });
                continue;
            }

            var candidate = new MutationCandidate(
                mutation.File,
                mutation.Line,
                mutation.Operator,
                mutation.Original,
                mutation.Mutated);

            try
            {
                Mutator.Apply(candidate);
                var testResult = _runner.RunTests(sourcePath, testPath, options.StubsPath);
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
            catch (Exception ex)
            {
                Console.Error.WriteLine($"Unexpected error replaying {mutation.Id}: {ex.GetType().Name}: {ex.Message}");
                try { GitService.RestoreFile(candidate.File); } catch { /* best effort */ }
                status = MutationStatus.CompileError;
            }

            var statusLabel = status switch
            {
                MutationStatus.Killed => "KILLED",
                MutationStatus.Survived => "SURVIVED",
                MutationStatus.CompileError => "COMPILE_ERROR",
                MutationStatus.TimedOut => "TIMED_OUT",
                _ => status.ToString().ToUpperInvariant()
            };
            Log($"  [{index}/{total}] {mutation.Id} [{mutation.Operator}] {Path.GetFileName(mutation.File)}:{mutation.Line} → {statusLabel}");

            results.Add(mutation with { Status = status, CaughtBy = caughtBy });
        }

        // Append run and save log
        log.AppendRun(results);
        log.Save();

        // Generate report
        var score = ReportGenerator.CalculateScore(results);
        var report = ReportGenerator.GenerateMarkdown(results, sourcePath);

        return new PipelineResult(0, results, score, report);
    }

    /// <summary>
    /// Checks whether a file's line at <paramref name="line"/> (1-based) matches
    /// <paramref name="original"/>. Returns false if the line is out of range or
    /// the content differs (mutation is OBSOLETE).
    /// </summary>
    private static bool FileContainsOriginal(string filePath, int line, string original)
    {
        if (!File.Exists(filePath))
            return false;

        var lines = File.ReadAllLines(filePath);
        if (line < 1 || line > lines.Length)
            return false;

        return lines[line - 1] == original;
    }

    private static string FormatElapsed(TimeSpan elapsed) =>
        elapsed.TotalSeconds >= 1
            ? $"{elapsed.TotalSeconds:F1}s"
            : $"{elapsed.TotalMilliseconds:F0}ms";

    /// <summary>
    /// Kills the current al-runner server and starts a fresh one.
    /// Called after a timeout or unexpected exception to prevent stdout/stdin
    /// protocol desynchronization from affecting subsequent mutations.
    /// </summary>
    private static async Task<(AlRunnerServer Server, ITestRunner Runner)> RestartServerAsync(
        AlRunnerServer oldServer, string alRunnerPath, Action<string> log)
    {
        log("Restarting al-runner server...");
        try { await oldServer.DisposeAsync(); } catch { /* already dead */ }
        var newServer = await AlRunnerServer.StartAsync(alRunnerPath);
        return (newServer, newServer);
    }
}
