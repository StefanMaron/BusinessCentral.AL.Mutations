using System.Diagnostics;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace AlMutate.Services;

public class AlRunnerTestRunner : ITestRunner
{
    public TestRunResult RunTests(string sourcePath, string testPath, string? stubsPath = null)
    {
        EnsureAlRunnerInstalled();

        // Build the file list: find all *.al files in each directory
        var alFiles = new List<string>();
        alFiles.AddRange(Directory.GetFiles(sourcePath, "*.al", SearchOption.AllDirectories));
        if (stubsPath != null)
            alFiles.AddRange(Directory.GetFiles(stubsPath, "*.al", SearchOption.AllDirectories));
        alFiles.AddRange(Directory.GetFiles(testPath, "*.al", SearchOption.AllDirectories));

        // Run: al-runner --output-json <files...>
        var quotedFiles = string.Join(" ", alFiles.Select(f => $"\"{f}\""));
        var args = $"--output-json {quotedFiles}";
        var (exitCode, stdout, _) = RunProcess("al-runner", args);

        // Parse JSON output
        // Format: {"exitCode":0,"passed":3,"failed":0,"errors":0,"tests":[{"name":"...","status":"pass",...}]}
        AlRunnerOutput? result = null;
        if (!string.IsNullOrWhiteSpace(stdout))
        {
            try
            {
                result = JsonSerializer.Deserialize<AlRunnerOutput>(stdout, JsonOptions);
            }
            catch (JsonException)
            {
                // If JSON parse fails, treat as compile error
            }
        }

        bool compileError = (result?.Tests is null || result.Tests.Count == 0) && exitCode != 0;
        var failedTests = result?.Tests?
            .Where(t => t.Status != "pass")
            .Select(t => t.Name)
            .ToList() ?? [];

        return new TestRunResult(
            Passed: exitCode == 0,
            FailedTests: failedTests,
            CompileError: compileError);
    }

    private static void EnsureAlRunnerInstalled()
    {
        // Check if al-runner is available
        var (exitCode, _, _) = RunProcess("al-runner", "--version", ignoreErrors: true);
        if (exitCode == 0) return;

        // Install it
        Console.WriteLine("al-runner not found. Installing msdyn365bc.al.runner...");
        var (installCode, _, stderr) = RunProcess("dotnet", "tool install --global msdyn365bc.al.runner");
        if (installCode != 0)
            throw new MutationException($"Failed to install al-runner: {stderr}");
    }

    private static (int ExitCode, string Stdout, string Stderr) RunProcess(
        string fileName, string arguments, bool ignoreErrors = false)
    {
        try
        {
            var psi = new ProcessStartInfo(fileName, arguments)
            {
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
            };

            using var proc = Process.Start(psi)
                ?? throw new MutationException($"Failed to start process: {fileName}");

            var stdout = proc.StandardOutput.ReadToEnd();
            var stderr = proc.StandardError.ReadToEnd();
            proc.WaitForExit();

            return (proc.ExitCode, stdout, stderr);
        }
        catch (MutationException)
        {
            throw;
        }
        catch (Exception ex)
        {
            if (ignoreErrors)
                return (1, string.Empty, ex.Message);
            throw new MutationException($"'{fileName}' is not available or could not be executed: {ex.Message}");
        }
    }

    // JSON model for al-runner --output-json output
    private record AlRunnerOutput(
        [property: JsonPropertyName("exitCode")] int ExitCode,
        [property: JsonPropertyName("tests")] List<AlRunnerTest>? Tests);

    private record AlRunnerTest(
        [property: JsonPropertyName("name")] string Name,
        [property: JsonPropertyName("status")] string Status);

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };
}
