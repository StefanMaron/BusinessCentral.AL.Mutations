namespace AlMutate.Models;

public record PipelineOptions
{
    public string Command { get; init; } = "";
    public string SourcePath { get; init; } = "";
    public string? TestPath { get; init; }
    public string? StubsPath { get; init; }
    public string? OperatorsPath { get; init; }
    public string? LogFilePath { get; init; }
    public int? MaxMutations { get; init; }
    /// <summary>When true, suppress all progress output; only errors go to stderr.</summary>
    public bool Silent { get; init; }
    /// <summary>
    /// Maximum time allowed for a single mutation's test run (apply → test → restore).
    /// When exceeded the mutation is recorded as COMPILE_ERROR and the loop continues.
    /// Null means no limit (not recommended for projects that could loop).
    /// </summary>
    public TimeSpan? MutationTimeout { get; init; }
}
