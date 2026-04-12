using System.Reflection;
using System.Text.Json;
using AlMutate.Models;

namespace AlMutate.Services;

public class OperatorLoader
{
    private static readonly HashSet<string> ValidCategories = new(StringComparer.OrdinalIgnoreCase)
    {
        "relational",
        "arithmetic",
        "logical",
        "boolean",
        "statement-removal",
        "boundary",
        "control-flow",
        "bc-specific"
    };

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        AllowTrailingCommas = true,
        ReadCommentHandling = JsonCommentHandling.Skip
    };

    /// <summary>
    /// Loads operators from the given file path, or from the embedded default resource if path is null.
    /// </summary>
    public List<MutationOperator> Load(string? path = null)
    {
        if (path is null)
        {
            return LoadFromEmbeddedResource();
        }

        if (!File.Exists(path))
            throw new FileNotFoundException($"Operator file not found: {path}", path);

        using var stream = File.OpenRead(path);
        return LoadFromStream(stream);
    }

    /// <summary>
    /// Deserializes operators from a JSON stream.
    /// </summary>
    public List<MutationOperator> LoadFromStream(Stream stream)
    {
        var file = JsonSerializer.Deserialize<OperatorFile>(stream, JsonOptions)
            ?? throw new InvalidOperationException("Failed to deserialize operator file: result was null.");

        return file.Operators.ToList();
    }

    /// <summary>
    /// Validates operators and returns a list of error messages (empty means valid).
    /// </summary>
    public List<string> Validate(List<MutationOperator> operators)
    {
        var errors = new List<string>();

        var ids = new HashSet<string>();
        foreach (var op in operators)
        {
            if (string.IsNullOrWhiteSpace(op.Id))
                errors.Add($"Operator has missing or empty Id.");

            if (string.IsNullOrWhiteSpace(op.Name))
                errors.Add($"Operator '{op.Id}' has missing or empty Name.");

            if (string.IsNullOrWhiteSpace(op.Category))
                errors.Add($"Operator '{op.Id}' has missing or empty Category.");
            else if (!ValidCategories.Contains(op.Category))
                errors.Add($"Operator '{op.Id}' has invalid category '{op.Category}'. Valid categories: {string.Join(", ", ValidCategories)}.");

            if (string.IsNullOrWhiteSpace(op.NodeType))
                errors.Add($"Operator '{op.Id}' has missing or empty NodeType.");

            if (!string.IsNullOrWhiteSpace(op.Id))
            {
                if (!ids.Add(op.Id))
                    errors.Add($"Duplicate operator Id: '{op.Id}'.");
            }
        }

        return errors;
    }

    private List<MutationOperator> LoadFromEmbeddedResource()
    {
        var assembly = Assembly.GetExecutingAssembly();
        const string resourceName = "AlMutate.operators.default.json";

        using var stream = assembly.GetManifestResourceStream(resourceName)
            ?? throw new InvalidOperationException(
                $"Embedded resource '{resourceName}' not found. " +
                $"Available resources: {string.Join(", ", assembly.GetManifestResourceNames())}");

        return LoadFromStream(stream);
    }
}
