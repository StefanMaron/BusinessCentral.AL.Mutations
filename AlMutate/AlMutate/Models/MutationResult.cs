using System.Text.Json.Serialization;

namespace AlMutate.Models;

public enum MutationStatus { Killed, Survived, CompileError, Obsolete }

public record MutationResult(
    [property: JsonPropertyName("id")] string Id,
    [property: JsonPropertyName("operator")] string Operator,
    [property: JsonPropertyName("file")] string File,
    [property: JsonPropertyName("line")] int Line,
    [property: JsonPropertyName("original")] string Original,
    [property: JsonPropertyName("mutated")] string Mutated,
    [property: JsonPropertyName("status")] MutationStatus Status,
    [property: JsonPropertyName("caught_by")] string? CaughtBy);
