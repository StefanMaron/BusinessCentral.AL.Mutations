using System.Text.Json.Serialization;

namespace AlMutate.Models;

public record MutationLogFile(
    [property: JsonPropertyName("schema_version")] int SchemaVersion,
    [property: JsonPropertyName("project")] string Project,
    [property: JsonPropertyName("runs")] List<MutationRun> Runs);
