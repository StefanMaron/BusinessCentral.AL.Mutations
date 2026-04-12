namespace AlMutate.Services;

public record TestRunResult(bool Passed, List<string> FailedTests, bool CompileError = false);

public interface ITestRunner
{
    TestRunResult RunTests(string sourcePath, string testPath, string? stubsPath = null);
}
