using System.Text.Json.Serialization;

namespace AlMutate.Models;

public record MutationOperator(
    [property: JsonPropertyName("id")] string Id,
    [property: JsonPropertyName("name")] string Name,
    [property: JsonPropertyName("category")] string Category,
    [property: JsonPropertyName("node_type")] string NodeType,
    [property: JsonPropertyName("operator_token")] string? OperatorToken,
    [property: JsonPropertyName("identifier")] string? Identifier,
    [property: JsonPropertyName("argument_match")] string? ArgumentMatch,
    [property: JsonPropertyName("identifier_replacement")] string? IdentifierReplacement,
    [property: JsonPropertyName("replacement")] string? Replacement,
    [property: JsonPropertyName("skip_string_operands")] bool SkipStringOperands = false)
{
    public bool IsStatementRemoval => Replacement is null && IdentifierReplacement is null;
}

public record OperatorFile(
    [property: JsonPropertyName("operators")] MutationOperator[] Operators);
