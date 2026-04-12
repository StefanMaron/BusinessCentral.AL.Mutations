using System.IO;
using AlMutate.Models;
using AlMutate.Services;
using Xunit;

namespace AlMutate.Tests;

public class AlScannerTests
{
    private static readonly string FixturesDir = Path.Combine(
        Path.GetDirectoryName(typeof(AlScannerTests).Assembly.Location)!,
        "fixtures");

    private static readonly string SampleFile = Path.Combine(FixturesDir, "sample.al");
    private static readonly string NoMatchesFile = Path.Combine(FixturesDir, "NoMatches.al");

    private static List<MutationOperator> DefaultOperators()
    {
        return new OperatorLoader().Load();
    }

    // -----------------------------------------------------------------------
    // Basic scan tests
    // -----------------------------------------------------------------------

    [Fact]
    public void ScanFile_Sample_FindsMutations()
    {
        var candidates = AlScanner.ScanFile(SampleFile, DefaultOperators());

        Assert.NotEmpty(candidates);
    }

    [Fact]
    public void ScanFile_Candidate_HasRequiredFields()
    {
        var candidates = AlScanner.ScanFile(SampleFile, DefaultOperators());

        foreach (var c in candidates)
        {
            Assert.False(string.IsNullOrEmpty(c.File), "File must not be empty");
            Assert.True(c.Line > 0, $"Line must be > 0 (was {c.Line})");
            Assert.False(string.IsNullOrEmpty(c.OperatorId), "OperatorId must not be empty");
            Assert.NotNull(c.Original);
            Assert.NotNull(c.Mutated);
        }
    }

    [Fact]
    public void ScanFile_FindsRelationalOperators()
    {
        var candidates = AlScanner.ScanFile(SampleFile, DefaultOperators());

        Assert.Contains(candidates, c => c.OperatorId.StartsWith("rel-"));
    }

    [Fact]
    public void ScanFile_SkipsLineComments()
    {
        // Lines that are pure comments (// ...) should produce no candidates
        var candidates = AlScanner.ScanFile(SampleFile, DefaultOperators());

        // Comment lines in sample.al contain things like "// Amount > 0" -
        // no candidate should have Original that is a comment line
        foreach (var c in candidates)
        {
            var trimmed = c.Original.TrimStart();
            Assert.False(trimmed.StartsWith("//"), $"Candidate on line {c.Line} appears to be a comment line: '{c.Original}'");
        }
    }

    [Fact]
    public void ScanFile_SkipsStringLiterals()
    {
        // No relational candidate should come from inside a string literal.
        // The sample has Error('Credit limit exceeded.') — no relational inside it.
        var candidates = AlScanner.ScanFile(SampleFile, DefaultOperators());
        var relational = candidates.Where(c => c.OperatorId.StartsWith("rel-")).ToList();

        // None of the relational operators should have their Mutated text
        // starting with a quote character (which would indicate a string context)
        foreach (var c in relational)
        {
            var trimmed = c.Original.TrimStart();
            // We just verify it is not a line that consists of only a string literal
            Assert.False(trimmed.StartsWith("'") || trimmed.StartsWith("\""),
                $"Relational candidate seems to be inside a string at line {c.Line}");
        }
    }

    // -----------------------------------------------------------------------
    // Specific operator tests with known line numbers
    // -----------------------------------------------------------------------

    [Fact]
    public void ScanFile_FindsGtOnKnownLine()
    {
        // Line 9 in sample.al: if Amount > 0 then begin
        var candidates = AlScanner.ScanFile(SampleFile, DefaultOperators());

        var hit = candidates.FirstOrDefault(c => c.OperatorId == "rel-gt-to-gte" && c.Line == 9);
        Assert.NotNull(hit);
        Assert.Contains(">=", hit.Mutated);
    }

    [Fact]
    public void ScanFile_FindsGteOnKnownLine()
    {
        // Line 11 in sample.al: if Amount >= 1000 then
        var candidates = AlScanner.ScanFile(SampleFile, DefaultOperators());

        var hit = candidates.FirstOrDefault(c => c.OperatorId == "rel-gte-to-gt" && c.Line == 11);
        Assert.NotNull(hit);
    }

    [Fact]
    public void ScanFile_FindsAdditiveOnKnownLine()
    {
        // Line 21 in sample.al: Amount := Amount + 100;
        var candidates = AlScanner.ScanFile(SampleFile, DefaultOperators());

        var hit = candidates.FirstOrDefault(c => c.OperatorId == "arith-add-to-sub" && c.Line == 21);
        Assert.NotNull(hit);
        Assert.Contains("-", hit.Mutated);
    }

    [Fact]
    public void ScanFile_FindsLogicalAnd()
    {
        // Line 40 in sample.al: if Quantity > 10 and Price > 100.0 then
        var candidates = AlScanner.ScanFile(SampleFile, DefaultOperators());

        var hit = candidates.FirstOrDefault(c => c.OperatorId == "logic-and-to-or");
        Assert.NotNull(hit);
        Assert.Contains("or", hit.Mutated);
    }

    [Fact]
    public void ScanFile_FindsStatementRemoval()
    {
        var candidates = AlScanner.ScanFile(SampleFile, DefaultOperators());

        var removal = candidates.FirstOrDefault(c => c.OperatorId.StartsWith("stmt-remove-"));
        Assert.NotNull(removal);
        Assert.StartsWith("//", removal.Mutated.TrimStart());
    }

    [Fact]
    public void ScanFile_FindsBoolTrueToFalse()
    {
        // Line 56 in sample.al: IsValid := true;
        var candidates = AlScanner.ScanFile(SampleFile, DefaultOperators());

        var hit = candidates.FirstOrDefault(c => c.OperatorId == "bool-true-to-false");
        Assert.NotNull(hit);
    }

    [Fact]
    public void ScanFile_FindsBoolFalseToTrue()
    {
        // Line 66 in sample.al: Found := false;
        var candidates = AlScanner.ScanFile(SampleFile, DefaultOperators());

        var hit = candidates.FirstOrDefault(c => c.OperatorId == "bool-false-to-true");
        Assert.NotNull(hit);
    }

    [Fact]
    public void ScanFile_FindsUnaryRemoveNot()
    {
        // Line 69 in sample.al: if not Rec.IsEmpty() then begin
        var candidates = AlScanner.ScanFile(SampleFile, DefaultOperators());

        var hit = candidates.FirstOrDefault(c => c.OperatorId == "unary-remove-not");
        Assert.NotNull(hit);
    }

    [Fact]
    public void ScanFile_StatementRemoval_MutatedStartsWithSlashes()
    {
        var candidates = AlScanner.ScanFile(SampleFile, DefaultOperators());
        var removals = candidates.Where(c => c.OperatorId.StartsWith("stmt-remove-")).ToList();

        Assert.NotEmpty(removals);
        foreach (var r in removals)
        {
            var trimmedMutated = r.Mutated.TrimStart();
            Assert.True(trimmedMutated.StartsWith("//"),
                $"Statement removal mutated line should start with // but was: '{r.Mutated}'");
        }
    }

    [Fact]
    public void ScanFile_Original_MatchesActualFileLine()
    {
        var lines = File.ReadAllLines(SampleFile);
        var candidates = AlScanner.ScanFile(SampleFile, DefaultOperators());

        foreach (var c in candidates)
        {
            var actualLine = lines[c.Line - 1]; // 1-based to 0-based
            Assert.Equal(actualLine, c.Original);
        }
    }

    [Fact]
    public void ScanFile_Mutated_ContainsExpectedReplacement()
    {
        var candidates = AlScanner.ScanFile(SampleFile, DefaultOperators());

        // Check a few specific operator replacements
        var gtToGte = candidates.FirstOrDefault(c => c.OperatorId == "rel-gt-to-gte");
        if (gtToGte != null)
        {
            Assert.Contains(">=", gtToGte.Mutated);
            Assert.DoesNotContain(" > ", gtToGte.Mutated); // original > replaced
        }

        var addToSub = candidates.FirstOrDefault(c => c.OperatorId == "arith-add-to-sub");
        if (addToSub != null)
        {
            Assert.Contains("-", addToSub.Mutated);
        }
    }

    // -----------------------------------------------------------------------
    // No-match file
    // -----------------------------------------------------------------------

    [Fact]
    public void ScanFile_NoMatches_ReturnsEmpty()
    {
        var candidates = AlScanner.ScanFile(NoMatchesFile, DefaultOperators());

        Assert.Empty(candidates);
    }

    // -----------------------------------------------------------------------
    // Directory scanning
    // -----------------------------------------------------------------------

    [Fact]
    public void ScanDirectory_FindsOnlyAlFiles()
    {
        var candidates = AlScanner.ScanDirectory(FixturesDir, DefaultOperators());

        // All candidate files must end in .al
        foreach (var c in candidates)
            Assert.EndsWith(".al", c.File, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void ScanDirectory_ScansSubdirectories()
    {
        // Create a temp dir with a nested .al file, verify scanner finds it
        var tempDir = Path.Combine(Path.GetTempPath(), "almutate_test_" + Guid.NewGuid().ToString("N"));
        var subDir = Path.Combine(tempDir, "sub");
        Directory.CreateDirectory(subDir);

        try
        {
            // Write a minimal AL file with a mutation target in the subdirectory
            var nestedFile = Path.Combine(subDir, "nested.al");
            File.WriteAllText(nestedFile, @"codeunit 50200 ""Nested""
{
    procedure Foo(Amount: Decimal)
    begin
        if Amount > 0 then
            exit;
    end;
}");

            var candidates = AlScanner.ScanDirectory(tempDir, DefaultOperators());

            Assert.NotEmpty(candidates);
            Assert.All(candidates, c => Assert.Equal(
                Path.GetFullPath(nestedFile),
                Path.GetFullPath(c.File)));
        }
        finally
        {
            Directory.Delete(tempDir, recursive: true);
        }
    }

    // -----------------------------------------------------------------------
    // Additional specific operator tests
    // -----------------------------------------------------------------------

    [Fact]
    public void ScanFile_FindsMultiplicative()
    {
        // Line 42 in sample.al: exit(Quantity * Price * 0.9)
        var candidates = AlScanner.ScanFile(SampleFile, DefaultOperators());

        var hit = candidates.FirstOrDefault(c => c.OperatorId == "arith-mul-to-div");
        Assert.NotNull(hit);
    }

    [Fact]
    public void ScanFile_FindsNeq()
    {
        // Line 52 in sample.al: if Balance <> 0 then
        var candidates = AlScanner.ScanFile(SampleFile, DefaultOperators());

        var hit = candidates.FirstOrDefault(c => c.OperatorId == "rel-neq-to-eq");
        Assert.NotNull(hit);
    }

    [Fact]
    public void ScanFile_FindsBcSpecific()
    {
        // Line 34 in sample.al: Rec.Modify(true)  -> bc-modify-trigger-true
        var candidates = AlScanner.ScanFile(SampleFile, DefaultOperators());

        var hit = candidates.FirstOrDefault(c => c.OperatorId.StartsWith("bc-"));
        Assert.NotNull(hit);
    }
}
