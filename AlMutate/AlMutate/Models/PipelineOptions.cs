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
}
