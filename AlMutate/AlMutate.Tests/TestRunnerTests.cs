using AlMutate.Services;
using Xunit;

namespace AlMutate.Tests;

// Simple fake implementation for testing the interface contract
internal class FakeTestRunner : ITestRunner
{
    private readonly bool _passed;
    private readonly List<string> _failedTests;
    private readonly bool _compileError;

    public FakeTestRunner(bool passed, List<string>? failedTests = null, bool compileError = false)
    {
        _passed = passed;
        _failedTests = failedTests ?? new List<string>();
        _compileError = compileError;
    }

    public TestRunResult RunTests(string sourcePath, string testPath, string? stubsPath = null)
        => new TestRunResult(_passed, _failedTests, _compileError);
}

public class TestRunnerTests
{
    // -----------------------------------------------------------------------
    // ITestRunner_AllPass_ReturnsTrue
    // -----------------------------------------------------------------------
    [Fact]
    public void ITestRunner_AllPass_ReturnsTrue()
    {
        ITestRunner runner = new FakeTestRunner(true);

        var result = runner.RunTests("./src", "./test.app");

        Assert.True(result.Passed);
    }

    // -----------------------------------------------------------------------
    // ITestRunner_SomeFail_ReturnsFalse
    // -----------------------------------------------------------------------
    [Fact]
    public void ITestRunner_SomeFail_ReturnsFalse()
    {
        ITestRunner runner = new FakeTestRunner(false, new List<string> { "MyFailingTest" });

        var result = runner.RunTests("./src", "./test.app");

        Assert.False(result.Passed);
    }

    // -----------------------------------------------------------------------
    // ITestRunner_SomeFail_ReturnsFailedNames
    // -----------------------------------------------------------------------
    [Fact]
    public void ITestRunner_SomeFail_ReturnsFailedNames()
    {
        var expectedName = "ValidateCreditLimit_Negative_ThrowsError";
        ITestRunner runner = new FakeTestRunner(false, new List<string> { expectedName });

        var result = runner.RunTests("./src", "./test.app");

        Assert.Contains(expectedName, result.FailedTests);
    }

    // -----------------------------------------------------------------------
    // TestRunResult_Passed_Property
    // -----------------------------------------------------------------------
    [Fact]
    public void TestRunResult_Passed_Property()
    {
        var result = new TestRunResult(true, new List<string>());

        Assert.True(result.Passed);
        Assert.Empty(result.FailedTests);
    }

    // -----------------------------------------------------------------------
    // TestRunResult_FailedTests_IsPopulated
    // -----------------------------------------------------------------------
    [Fact]
    public void TestRunResult_FailedTests_IsPopulated()
    {
        var failedTests = new List<string> { "TestA", "TestB" };
        var result = new TestRunResult(false, failedTests);

        Assert.Equal(2, result.FailedTests.Count);
        Assert.Contains("TestA", result.FailedTests);
        Assert.Contains("TestB", result.FailedTests);
    }

    // -----------------------------------------------------------------------
    // AlRunnerTestRunner_NonExistentPath_ReturnsCompileError
    // -----------------------------------------------------------------------
    [Fact]
    public void AlRunnerTestRunner_NonExistentPath_ReturnsCompileError()
    {
        var runner = new AlRunnerTestRunner();

        var result = runner.RunTests("/nonexistent/path/src", "/nonexistent/path/test");

        // Non-existent paths: pipeline returns exit code != 0, no tests collected
        Assert.False(result.Passed);
        Assert.True(result.CompileError);
    }

    // -----------------------------------------------------------------------
    // TestRunResult_CompileError_DefaultIsFalse
    // -----------------------------------------------------------------------
    [Fact]
    public void TestRunResult_CompileError_DefaultIsFalse()
    {
        var result = new TestRunResult(true, new List<string>());

        Assert.False(result.CompileError);
    }
}
