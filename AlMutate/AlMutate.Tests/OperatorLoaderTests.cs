using AlMutate.Models;
using AlMutate.Services;
using Xunit;

namespace AlMutate.Tests;

public class OperatorLoaderTests
{
    private readonly OperatorLoader _loader = new();

    [Fact]
    public void LoadDefaultOperators_ReturnsNonEmpty()
    {
        var operators = _loader.Load();

        Assert.NotEmpty(operators);
    }

    [Fact]
    public void LoadDefaultOperators_AllHaveRequiredFields()
    {
        var operators = _loader.Load();

        foreach (var op in operators)
        {
            Assert.False(string.IsNullOrEmpty(op.Id), $"Operator has empty Id");
            Assert.False(string.IsNullOrEmpty(op.Name), $"Operator '{op.Id}' has empty Name");
            Assert.False(string.IsNullOrEmpty(op.Category), $"Operator '{op.Id}' has empty Category");
            Assert.False(string.IsNullOrEmpty(op.NodeType), $"Operator '{op.Id}' has empty NodeType");
        }
    }

    [Fact]
    public void LoadDefaultOperators_AllIdsUnique()
    {
        var operators = _loader.Load();

        var distinctCount = operators.Select(o => o.Id).Distinct().Count();
        Assert.Equal(operators.Count, distinctCount);
    }

    [Fact]
    public void LoadDefaultOperators_HasExpectedCategories()
    {
        var operators = _loader.Load();
        var categories = operators.Select(o => o.Category).ToHashSet();

        Assert.Contains("relational", categories);
        Assert.Contains("arithmetic", categories);
        Assert.Contains("logical", categories);
        Assert.Contains("statement-removal", categories);
        Assert.Contains("bc-specific", categories);
    }

    [Fact]
    public void OperatorRecord_IsStatementRemoval_WhenReplacementNull()
    {
        var op = new MutationOperator(
            Id: "test-removal",
            Name: "Test removal",
            Category: "statement-removal",
            NodeType: "call_expression",
            OperatorToken: null,
            Identifier: "Error",
            ArgumentMatch: null,
            IdentifierReplacement: null,
            Replacement: null);

        Assert.True(op.IsStatementRemoval);
    }

    [Fact]
    public void OperatorRecord_IsNotStatementRemoval_WhenReplacementSet()
    {
        var op = new MutationOperator(
            Id: "rel-gt-to-gte",
            Name: "Greater-than to greater-or-equal",
            Category: "relational",
            NodeType: "comparison_expression",
            OperatorToken: ">",
            Identifier: null,
            ArgumentMatch: null,
            IdentifierReplacement: null,
            Replacement: ">=");

        Assert.False(op.IsStatementRemoval);
    }

    [Fact]
    public void LoadFromPath_NonexistentFile_Throws()
    {
        Assert.Throws<FileNotFoundException>(() =>
            _loader.Load("/nonexistent/path/operators.json"));
    }

    [Fact]
    public void Validate_MissingId_ReturnsError()
    {
        var operators = new List<MutationOperator>
        {
            new MutationOperator(
                Id: "",
                Name: "Some operator",
                Category: "relational",
                NodeType: "comparison_expression",
                OperatorToken: ">",
                Identifier: null,
                ArgumentMatch: null,
                IdentifierReplacement: null,
                Replacement: ">=")
        };

        var errors = _loader.Validate(operators);

        Assert.NotEmpty(errors);
        Assert.Contains(errors, e => e.Contains("Id"));
    }

    [Fact]
    public void Validate_InvalidCategory_ReturnsError()
    {
        var operators = new List<MutationOperator>
        {
            new MutationOperator(
                Id: "some-op",
                Name: "Some operator",
                Category: "not-a-valid-category",
                NodeType: "comparison_expression",
                OperatorToken: ">",
                Identifier: null,
                ArgumentMatch: null,
                IdentifierReplacement: null,
                Replacement: ">=")
        };

        var errors = _loader.Validate(operators);

        Assert.NotEmpty(errors);
        Assert.Contains(errors, e => e.Contains("category") || e.Contains("Category"));
    }

    [Fact]
    public void Validate_EmptyList_ReturnsNoErrors()
    {
        var errors = _loader.Validate(new List<MutationOperator>());

        Assert.Empty(errors);
    }
}
