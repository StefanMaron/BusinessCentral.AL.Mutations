using System.Diagnostics;

namespace AlMutate.Tests;

public static class CliRunner
{
    private static readonly string RepoRoot = Path.GetFullPath(
        Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", ".."));
    private static readonly string ProjectPath = Path.Combine(RepoRoot, "AlMutate", "AlMutate");

    public record CliResult(int ExitCode, string StdOut, string StdErr);

    public static async Task<CliResult> RunAsync(string args, int timeoutMs = 60_000)
    {
        var psi = new ProcessStartInfo
        {
            FileName = "dotnet",
            Arguments = $"run --no-build --project \"{ProjectPath}\" -- {args}",
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        using var process = Process.Start(psi)!;
        var stdoutTask = process.StandardOutput.ReadToEndAsync();
        var stderrTask = process.StandardError.ReadToEndAsync();

        var completed = await Task.WhenAny(
            Task.WhenAll(stdoutTask, stderrTask, process.WaitForExitAsync()),
            Task.Delay(timeoutMs));

        if (!process.HasExited)
        {
            process.Kill(entireProcessTree: true);
            await process.WaitForExitAsync();
        }

        return new CliResult(process.ExitCode, await stdoutTask, await stderrTask);
    }
}
