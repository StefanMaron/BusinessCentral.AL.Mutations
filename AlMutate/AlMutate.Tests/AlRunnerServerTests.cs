using AlMutate.Services;
using Xunit;

namespace AlMutate.Tests;

/// <summary>
/// Integration tests for <see cref="AlRunnerServer"/>.
/// These require al-runner to be installed and are excluded from the standard
/// unit-test run. Execute with:
///   dotnet test --filter Category=Integration
/// </summary>
[Collection("Integration")]
public class AlRunnerServerTests
{
    // -----------------------------------------------------------------------
    // AlRunnerServer_Start_ProcessRunning
    // -----------------------------------------------------------------------
    [Fact]
    [Trait("Category", "Integration")]
    public async Task AlRunnerServer_Start_ProcessRunning()
    {
        var alRunnerPath = AlRunnerTestRunner.GetAlRunnerPath();

        await using var server = await AlRunnerServer.StartAsync(alRunnerPath);

        Assert.NotNull(server);
    }

    // -----------------------------------------------------------------------
    // AlRunnerServer_Dispose_ShutsDownGracefully
    // -----------------------------------------------------------------------
    [Fact]
    [Trait("Category", "Integration")]
    public async Task AlRunnerServer_Dispose_ShutsDownGracefully()
    {
        var alRunnerPath = AlRunnerTestRunner.GetAlRunnerPath();

        // Start and immediately dispose — should not throw
        var server = await AlRunnerServer.StartAsync(alRunnerPath);
        await server.DisposeAsync();

        // If we get here without exception or hang, the server shut down gracefully.
        Assert.True(true);
    }

    // -----------------------------------------------------------------------
    // AlRunnerServer_RunTests_ReturnsResult
    // -----------------------------------------------------------------------
    [Fact]
    [Trait("Category", "Integration")]
    public async Task AlRunnerServer_RunTests_ReturnsResult()
    {
        // Use the 01-credit fixture (same paths as IntegrationTests)
        var repoRoot = Path.GetFullPath(
            Path.Combine(AppContext.BaseDirectory, "..", "..", "..", ".."));
        var fixtureSrc = Path.Combine(repoRoot, "tests", "01-credit", "src");
        var fixtureTest = Path.Combine(repoRoot, "tests", "01-credit", "test");

        var alRunnerPath = AlRunnerTestRunner.GetAlRunnerPath();

        await using var server = await AlRunnerServer.StartAsync(alRunnerPath);

        var result = server.RunTests(fixtureSrc, fixtureTest);

        // Must return a result — either tests passed (Passed) or compile failed
        // (CompileError). Either way, a TestRunResult is returned without throwing.
        Assert.NotNull(result);
        // On a valid fixture the baseline must pass
        Assert.True(result.Passed,
            $"Expected baseline to pass. CompileError={result.CompileError}, " +
            $"FailedTests=[{string.Join(", ", result.FailedTests)}]");
    }
}
