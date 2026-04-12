using AlRunner;

namespace AlMutate.Services;

public class AlRunnerTestRunner : ITestRunner
{
    private readonly AlRunnerPipeline _pipeline = new();

    public TestRunResult RunTests(string sourcePath, string testPath, string? stubsPath = null)
    {
        var options = new AlRunner.PipelineOptions
        {
            InputPaths = stubsPath != null
                ? [sourcePath, stubsPath, testPath]
                : [sourcePath, testPath]
        };
        var result = _pipeline.Run(options);

        bool compileError = result.Tests.Count == 0 && result.ExitCode != 0;
        var failedTests = result.Tests
            .Where(t => t.Status != TestStatus.Pass)
            .Select(t => t.Name)
            .ToList();

        return new TestRunResult(
            Passed: result.ExitCode == 0,
            FailedTests: failedTests,
            CompileError: compileError);
    }
}
