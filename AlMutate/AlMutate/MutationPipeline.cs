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
    /// </summary>
    public PipelineResult Run(PipelineOptions options)
    {
        // 1. Check git clean (relative to the source directory)
        if (!GitService.IsWorkingTreeClean(options.SourcePath))
            return new PipelineResult(1, new List<MutationResult>(), 0.0, null, "Working tree is dirty");

        // 2. Scan candidates
        var candidates = AlScanner.ScanDirectory(options.SourcePath, _operators);
        if (options.MaxMutations.HasValue)
            candidates = candidates.Take(options.MaxMutations.Value).ToList();

        // 3. Run baseline
        var baseline = _runner.RunTests(options.SourcePath, options.TestPath!, options.StubsPath);
        if (!baseline.Passed && !baseline.CompileError)
            return new PipelineResult(1, new List<MutationResult>(), 0.0, null, "Baseline tests failed");

        // 4. Load or create log
        var logPath = options.LogFilePath ?? "mutations.json";
        var log = File.Exists(logPath)
            ? MutationLog.Load(logPath)
            : MutationLog.Create(logPath, options.SourcePath);

        // 5. Run each mutation
        var results = new List<MutationResult>();
        foreach (var candidate in candidates)
        {
            var id = log.NextMutationId();
            MutationStatus status;
            string? caughtBy = null;

            try
            {
                Mutator.Apply(candidate);
                var testResult = _runner.RunTests(options.SourcePath, options.TestPath!, options.StubsPath);
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

        // 6. Append run and save log
        log.AppendRun(results);
        log.Save();

        // 7. Generate report
        var score = ReportGenerator.CalculateScore(results);
        var report = ReportGenerator.GenerateMarkdown(results, options.SourcePath);

        return new PipelineResult(0, results, score, report);
    }

    /// <summary>
    /// Replay survived mutations from a previous log.
    /// </summary>
    public PipelineResult Replay(PipelineOptions options)
    {
        throw new NotImplementedException("Replay not yet implemented");
    }
}
