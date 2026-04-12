using System.Diagnostics;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace AlMutate.Services;

/// <summary>
/// Wraps an al-runner --server process for incremental compilation.
/// Keeps compiled assemblies warm in the server's LRU cache between mutations,
/// so only the changed .al file re-transpiles on each run.
/// Protocol: newline-delimited JSON over stdin/stdout.
/// </summary>
internal sealed class AlRunnerServer : ITestRunner, IAsyncDisposable
{
    private readonly Process _process;

    private AlRunnerServer(Process process)
    {
        _process = process;
    }

    /// <summary>
    /// Start al-runner in --server mode and wait for the readiness line.
    /// </summary>
    public static async Task<AlRunnerServer> StartAsync(string alRunnerPath)
    {
        var psi = new ProcessStartInfo
        {
            FileName = alRunnerPath,
            Arguments = "--server",
            RedirectStandardInput = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true,
        };

        var proc = Process.Start(psi)
            ?? throw new MutationException($"Failed to start al-runner server process: {alRunnerPath}");

        // Forward stderr to Console.Error in background (download progress, warnings, etc.)
        _ = Task.Run(async () =>
        {
            string? line;
            while ((line = await proc.StandardError.ReadLineAsync()) != null)
                await Console.Error.WriteLineAsync(line);
        });

        // Wait for the ready signal: {"ready":true}
        var readyLine = await proc.StandardOutput.ReadLineAsync();
        if (readyLine == null)
            throw new MutationException("al-runner server exited before signaling readiness");

        return new AlRunnerServer(proc);
    }

    /// <summary>
    /// ITestRunner implementation — sends a runTests command to the server and
    /// maps the response to a <see cref="TestRunResult"/>.
    /// </summary>
    public TestRunResult RunTests(string sourcePath, string testPath, string? stubsPath = null)
    {
        // Build sourcePaths list: source dir, optional stubs dir, test dir.
        // The server scans directories internally for .al files.
        var sourcePaths = new List<string> { sourcePath };
        if (stubsPath != null)
            sourcePaths.Add(stubsPath);
        sourcePaths.Add(testPath);

        var request = JsonSerializer.Serialize(new ServerRunTestsRequest
        {
            Command = "runTests",
            SourcePaths = sourcePaths.ToArray()
        });

        // Send request and read response (synchronous over the async streams)
        _process.StandardInput.WriteLine(request);
        _process.StandardInput.Flush();

        var responseLine = _process.StandardOutput.ReadLine()
            ?? throw new MutationException("al-runner server closed stdout before responding");

        ServerRunTestsResponse? response;
        try
        {
            response = JsonSerializer.Deserialize<ServerRunTestsResponse>(responseLine, JsonOptions);
        }
        catch (JsonException ex)
        {
            throw new MutationException($"Failed to parse al-runner server response: {ex.Message}\nResponse: {responseLine}");
        }

        if (response == null)
            throw new MutationException($"al-runner server returned null response: {responseLine}");

        if (response.Error != null)
            throw new MutationException($"al-runner server error: {response.Error}");

        bool compileError = (response.Tests is null || response.Tests.Count == 0) && response.ExitCode != 0;
        var failedTests = response.Tests?
            .Where(t => t.Status != "pass")
            .Select(t => t.Name)
            .ToList() ?? [];

        return new TestRunResult(
            Passed: response.ExitCode == 0,
            FailedTests: failedTests,
            CompileError: compileError);
    }

    public async ValueTask DisposeAsync()
    {
        if (!_process.HasExited)
        {
            try
            {
                // Send graceful shutdown command
                await _process.StandardInput.WriteLineAsync("{\"command\":\"shutdown\"}");
                await _process.StandardInput.FlushAsync();

                // Give the process a moment to exit cleanly
                using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
                try
                {
                    await _process.WaitForExitAsync(cts.Token);
                }
                catch (OperationCanceledException)
                {
                    // Didn't exit in time — force kill
                    _process.Kill();
                    await _process.WaitForExitAsync();
                }
            }
            catch
            {
                // Best effort — if the process is already gone or stdin is broken, just kill
                try { _process.Kill(); } catch { /* ignored */ }
            }
        }
        _process.Dispose();
    }

    // ── JSON models ────────────────────────────────────────────────────────────

    private sealed class ServerRunTestsRequest
    {
        [JsonPropertyName("command")]
        public string Command { get; set; } = "runTests";

        [JsonPropertyName("sourcePaths")]
        public string[] SourcePaths { get; set; } = [];
    }

    private sealed class ServerRunTestsResponse
    {
        [JsonPropertyName("exitCode")]
        public int ExitCode { get; set; }

        [JsonPropertyName("passed")]
        public int Passed { get; set; }

        [JsonPropertyName("failed")]
        public int Failed { get; set; }

        [JsonPropertyName("errors")]
        public int Errors { get; set; }

        [JsonPropertyName("cached")]
        public bool Cached { get; set; }

        [JsonPropertyName("changedFiles")]
        public List<string>? ChangedFiles { get; set; }

        [JsonPropertyName("tests")]
        public List<ServerTestEntry>? Tests { get; set; }

        [JsonPropertyName("error")]
        public string? Error { get; set; }
    }

    private sealed class ServerTestEntry
    {
        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;

        [JsonPropertyName("status")]
        public string Status { get; set; } = string.Empty;

        [JsonPropertyName("durationMs")]
        public double DurationMs { get; set; }
    }

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };
}
