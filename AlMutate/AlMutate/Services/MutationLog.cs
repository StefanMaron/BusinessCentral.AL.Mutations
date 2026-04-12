using System.Text.Json;
using System.Text.Json.Serialization;
using AlMutate.Models;

namespace AlMutate.Services;

public class MutationLog
{
    private readonly string _filePath;
    private readonly MutationLogFile _data;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        Converters = { new UpperCaseEnumConverter<MutationStatus>() }
    };

    private MutationLog(string filePath, MutationLogFile data)
    {
        _filePath = filePath;
        _data = data;
    }

    /// <summary>Create a new empty log for the given project path.</summary>
    public static MutationLog Create(string filePath, string project)
    {
        var data = new MutationLogFile(
            SchemaVersion: 1,
            Project: project,
            Runs: new List<MutationRun>());
        return new MutationLog(filePath, data);
    }

    /// <summary>Load an existing log from disk.</summary>
    public static MutationLog Load(string filePath)
    {
        var json = File.ReadAllText(filePath);
        var data = JsonSerializer.Deserialize<MutationLogFile>(json, JsonOptions)
            ?? throw new InvalidOperationException($"Failed to deserialize mutation log: {filePath}");
        return new MutationLog(filePath, data);
    }

    /// <summary>Append a new run (auto-increments run number, uses current UTC time).</summary>
    public void AppendRun(List<MutationResult> results)
    {
        var nextRunNumber = _data.Runs.Count == 0 ? 1 : _data.Runs.Max(r => r.Run) + 1;
        var date = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ss");
        var run = new MutationRun(nextRunNumber, date, results);
        _data.Runs.Add(run);
    }

    /// <summary>Write to disk (to the path it was loaded/created from).</summary>
    public void Save()
    {
        var json = JsonSerializer.Serialize(_data, JsonOptions);
        File.WriteAllText(_filePath, json);
    }

    /// <summary>Returns all SURVIVED results from the most recent run.</summary>
    public List<MutationResult> GetSurvivedFromLastRun()
    {
        if (_data.Runs.Count == 0)
            return new List<MutationResult>();

        var lastRun = _data.Runs[_data.Runs.Count - 1];
        return lastRun.Mutations
            .Where(m => m.Status == MutationStatus.Survived)
            .ToList();
    }

    /// <summary>Returns the next mutation ID: "M001", "M002", etc. Counts across ALL runs.</summary>
    public string NextMutationId()
    {
        var total = _data.Runs.Sum(r => r.Mutations.Count);
        return $"M{total + 1:D3}";
    }
}

/// <summary>Serializes enum values as UPPERCASE strings (e.g. KILLED, COMPILE_ERROR).</summary>
public sealed class UpperCaseEnumConverter<T> : JsonConverter<T> where T : struct, Enum
{
    public override T Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        var value = reader.GetString()
            ?? throw new JsonException("Expected a string for enum value.");

        // Normalize: "COMPILE_ERROR" → "CompileError" style matching
        // Try direct parse first (handles PascalCase), then normalize from UPPER_CASE
        if (Enum.TryParse<T>(value, ignoreCase: true, out var result))
            return result;

        // Convert UPPER_CASE to PascalCase: "COMPILE_ERROR" → "CompileError"
        var pascalCase = string.Concat(
            value.Split('_')
                .Select(word => char.ToUpperInvariant(word[0]) + word.Substring(1).ToLowerInvariant()));

        if (Enum.TryParse<T>(pascalCase, ignoreCase: false, out var result2))
            return result2;

        throw new JsonException($"Unable to parse '{value}' as {typeof(T).Name}.");
    }

    public override void Write(Utf8JsonWriter writer, T value, JsonSerializerOptions options)
    {
        // Convert PascalCase enum name to UPPER_SNAKE_CASE
        var name = value.ToString();
        var upperSnake = string.Concat(
            name.Select((c, i) =>
                i > 0 && char.IsUpper(c)
                    ? "_" + c
                    : c.ToString()))
            .ToUpperInvariant();
        writer.WriteStringValue(upperSnake);
    }
}
