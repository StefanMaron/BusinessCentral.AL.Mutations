using System.IO;
using AlMutate;
using AlMutate.Models;
using AlMutate.Services;
using Xunit;

namespace AlMutate.Tests;

public class MutatorTests : IDisposable
{
    private readonly string _tempFile;

    // A small 5-line AL snippet used by all tests
    private static readonly string[] AlLines =
    [
        "codeunit 50100 \"Credit Check\"",
        "{",
        "    procedure Check(Amount: Decimal)",
        "    begin",
        "        if Amount > 0 then",
    ];

    public MutatorTests()
    {
        _tempFile = Path.GetTempFileName();
        File.WriteAllLines(_tempFile, AlLines);
    }

    public void Dispose()
    {
        if (File.Exists(_tempFile))
            File.Delete(_tempFile);
    }

    // Helper — builds a candidate targeting line 5 (the "if Amount > 0" line)
    private MutationCandidate MakeCandidate(
        string original = "        if Amount > 0 then",
        string mutated = "        if Amount >= 0 then",
        int line = 5)
        => new MutationCandidate(_tempFile, line, "rel-gt-to-gte", original, mutated);

    // -----------------------------------------------------------------------

    [Fact]
    public void Apply_ReplacesTargetLine()
    {
        var candidate = MakeCandidate();
        Mutator.Apply(candidate);

        var lines = File.ReadAllLines(_tempFile);
        Assert.Equal(candidate.Mutated, lines[candidate.Line - 1]);
    }

    [Fact]
    public void Apply_OnlyChangesTargetLine()
    {
        var candidate = MakeCandidate();
        Mutator.Apply(candidate);

        var lines = File.ReadAllLines(_tempFile);
        for (int i = 0; i < lines.Length; i++)
        {
            if (i + 1 == candidate.Line)
                continue; // skip the mutated line
            Assert.Equal(AlLines[i], lines[i]);
        }
    }

    [Fact]
    public void Apply_StatementRemoval_CommentedOut()
    {
        // Mutated line starts with "//" (statement removal)
        var candidate = MakeCandidate(
            original: "        if Amount > 0 then",
            mutated: "        // if Amount > 0 then",
            line: 5);

        Mutator.Apply(candidate);

        var lines = File.ReadAllLines(_tempFile);
        Assert.StartsWith("//", lines[candidate.Line - 1].TrimStart());
    }

    [Fact]
    public void Apply_OriginalMismatch_ThrowsMutationException()
    {
        var candidate = MakeCandidate(
            original: "        if Amount < 0 then",   // wrong — actual line says ">"
            mutated: "        if Amount <= 0 then",
            line: 5);

        Assert.Throws<MutationException>(() => Mutator.Apply(candidate));
    }

    [Fact]
    public void Apply_LineOutOfRange_ThrowsMutationException()
    {
        var candidate = MakeCandidate(line: 999);   // file only has 5 lines

        Assert.Throws<MutationException>(() => Mutator.Apply(candidate));
    }
}
