using AlMutate.Models;

namespace AlMutate.Services;

public static class Mutator
{
    /// <summary>
    /// Reads the file, verifies line <paramref name="candidate"/>.Line matches
    /// <paramref name="candidate"/>.Original, replaces that line with
    /// <paramref name="candidate"/>.Mutated, and writes the file back.
    /// Line numbers are 1-based.
    /// </summary>
    /// <exception cref="MutationException">
    /// Thrown when the line number is out of range or the original text does not match.
    /// </exception>
    public static void Apply(MutationCandidate candidate)
    {
        // Read preserving line endings as individual lines
        var text = File.ReadAllText(candidate.File);

        // Split while keeping the newline suffix for each line so we can
        // reconstruct the file with original line endings intact.
        var rawLines = SplitKeepingEndings(text);

        if (candidate.Line < 1 || candidate.Line > rawLines.Count)
            throw new MutationException(
                $"Line {candidate.Line} is out of range (file has {rawLines.Count} lines): {candidate.File}");

        var idx = candidate.Line - 1;
        var currentContent = rawLines[idx].TrimEnd('\r', '\n');

        if (currentContent != candidate.Original)
            throw new MutationException(
                $"Original mismatch on line {candidate.Line} of {candidate.File}.\n" +
                $"  Expected: {candidate.Original}\n" +
                $"  Actual:   {currentContent}");

        // Preserve the line ending suffix (e.g. "\r\n" or "\n")
        var ending = rawLines[idx].Substring(currentContent.Length);
        rawLines[idx] = candidate.Mutated + ending;

        File.WriteAllText(candidate.File, string.Concat(rawLines));
    }

    // -----------------------------------------------------------------------
    // Helpers

    /// <summary>
    /// Splits a string into lines, with each element retaining its trailing
    /// newline characters (\n or \r\n). The final element may have no newline.
    /// </summary>
    private static List<string> SplitKeepingEndings(string text)
    {
        var result = new List<string>();
        int start = 0;
        for (int i = 0; i < text.Length; i++)
        {
            if (text[i] == '\n')
            {
                result.Add(text.Substring(start, i - start + 1));
                start = i + 1;
            }
        }
        if (start < text.Length)
            result.Add(text.Substring(start));
        return result;
    }
}
