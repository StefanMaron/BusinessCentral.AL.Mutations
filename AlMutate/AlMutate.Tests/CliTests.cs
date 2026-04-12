using Xunit;

namespace AlMutate.Tests;

/// <summary>
/// CLI integration tests. These require the project to be built first.
/// Each test invokes al-mutate via `dotnet run --no-build`.
/// </summary>
public class CliTests
{
    private static readonly string FixturesDir = Path.Combine(
        AppContext.BaseDirectory, "fixtures");

    // -----------------------------------------------------------------------
    // Cli_NoArgs_PrintsHelp
    // -----------------------------------------------------------------------
    [Fact]
    public async Task Cli_NoArgs_PrintsHelp()
    {
        var result = await CliRunner.RunAsync("");

        // No-args should print usage/help; either nonzero exit or usage text
        var combined = result.StdOut + result.StdErr;
        var hasHelp = combined.Contains("scan", StringComparison.OrdinalIgnoreCase)
                   || combined.Contains("run", StringComparison.OrdinalIgnoreCase)
                   || combined.Contains("Usage", StringComparison.OrdinalIgnoreCase)
                   || result.ExitCode != 0;

        Assert.True(hasHelp, $"Expected help output or nonzero exit. Got exit={result.ExitCode}, out='{result.StdOut}', err='{result.StdErr}'");
    }

    // -----------------------------------------------------------------------
    // Cli_Scan_PrintsCandidates
    // -----------------------------------------------------------------------
    [Fact]
    public async Task Cli_Scan_PrintsCandidates()
    {
        var result = await CliRunner.RunAsync($"scan \"{FixturesDir}\"");

        Assert.Equal(0, result.ExitCode);
        // The output should contain at least one operator ID (format: S001  [operator-id])
        Assert.Contains("[", result.StdOut);
    }

    // -----------------------------------------------------------------------
    // Cli_InvalidCommand_ExitNonZero
    // -----------------------------------------------------------------------
    [Fact]
    public async Task Cli_InvalidCommand_ExitNonZero()
    {
        var result = await CliRunner.RunAsync("unknown-command");

        Assert.NotEqual(0, result.ExitCode);
    }

    // -----------------------------------------------------------------------
    // Cli_Scan_Help_PrintsOptions
    // -----------------------------------------------------------------------
    [Fact]
    public async Task Cli_Scan_Help_PrintsOptions()
    {
        var result = await CliRunner.RunAsync("scan --help");

        Assert.Equal(0, result.ExitCode);
        var combined = result.StdOut + result.StdErr;
        Assert.Contains("--operators", combined, StringComparison.OrdinalIgnoreCase);
    }
}
