using AlMutate.Models;
using AlMutate.Services;
using Xunit;

namespace AlMutate.Tests;

public class ReportGeneratorTests
{
    // -----------------------------------------------------------------------
    // CalculateScore_MixedResults_CorrectPercentage
    // 2 killed, 1 survived → 66.67
    // -----------------------------------------------------------------------
    [Fact]
    public void CalculateScore_MixedResults_CorrectPercentage()
    {
        var results = new List<MutationResult>
        {
            new("M001", "op1", "f.al", 1, "orig", "mut", MutationStatus.Killed, "T1"),
            new("M002", "op2", "f.al", 2, "orig", "mut", MutationStatus.Killed, "T2"),
            new("M003", "op3", "f.al", 3, "orig", "mut", MutationStatus.Survived, null),
        };

        var score = ReportGenerator.CalculateScore(results);

        Assert.Equal(66.67, score, 2);
    }

    // -----------------------------------------------------------------------
    // CalculateScore_AllKilled_Returns100
    // -----------------------------------------------------------------------
    [Fact]
    public void CalculateScore_AllKilled_Returns100()
    {
        var results = new List<MutationResult>
        {
            new("M001", "op1", "f.al", 1, "orig", "mut", MutationStatus.Killed, "T1"),
            new("M002", "op2", "f.al", 2, "orig", "mut", MutationStatus.Killed, "T2"),
            new("M003", "op3", "f.al", 3, "orig", "mut", MutationStatus.Killed, "T3"),
        };

        var score = ReportGenerator.CalculateScore(results);

        Assert.Equal(100.0, score);
    }

    // -----------------------------------------------------------------------
    // CalculateScore_NoneKilled_Returns0
    // -----------------------------------------------------------------------
    [Fact]
    public void CalculateScore_NoneKilled_Returns0()
    {
        var results = new List<MutationResult>
        {
            new("M001", "op1", "f.al", 1, "orig", "mut", MutationStatus.Survived, null),
            new("M002", "op2", "f.al", 2, "orig", "mut", MutationStatus.Survived, null),
            new("M003", "op3", "f.al", 3, "orig", "mut", MutationStatus.Survived, null),
        };

        var score = ReportGenerator.CalculateScore(results);

        Assert.Equal(0.0, score);
    }

    // -----------------------------------------------------------------------
    // CalculateScore_EmptyResults_Returns100
    // -----------------------------------------------------------------------
    [Fact]
    public void CalculateScore_EmptyResults_Returns100()
    {
        var results = new List<MutationResult>();

        var score = ReportGenerator.CalculateScore(results);

        Assert.Equal(100.0, score);
    }

    // -----------------------------------------------------------------------
    // GenerateMarkdown_ContainsScore
    // -----------------------------------------------------------------------
    [Fact]
    public void GenerateMarkdown_ContainsScore()
    {
        var results = new List<MutationResult>
        {
            new("M001", "op1", "f.al", 1, "orig", "mut", MutationStatus.Killed, "T1"),
            new("M002", "op2", "f.al", 2, "orig", "mut", MutationStatus.Killed, "T2"),
            new("M003", "op3", "f.al", 3, "orig", "mut", MutationStatus.Survived, null),
        };

        var markdown = ReportGenerator.GenerateMarkdown(results, "./src");

        Assert.Contains("66.67%", markdown);
    }

    // -----------------------------------------------------------------------
    // GenerateMarkdown_ListsSurvivor
    // -----------------------------------------------------------------------
    [Fact]
    public void GenerateMarkdown_ListsSurvivor()
    {
        var results = new List<MutationResult>
        {
            new("M001", "op1", "f.al", 1, "orig", "mut", MutationStatus.Killed, "T1"),
            new("M002", "op2", "f.al", 2, "orig", "mut", MutationStatus.Killed, "T2"),
            new("M003", "op3", "f.al", 3, "orig", "mut", MutationStatus.Survived, null),
        };

        var markdown = ReportGenerator.GenerateMarkdown(results, "./src");

        Assert.Contains("M003", markdown);
    }
}
