using System.Text.Json.Serialization;

namespace AlMutate.Models;

public record MutationRun(
    [property: JsonPropertyName("run")] int Run,
    [property: JsonPropertyName("date")] string Date,
    [property: JsonPropertyName("mutations")] List<MutationResult> Mutations);
