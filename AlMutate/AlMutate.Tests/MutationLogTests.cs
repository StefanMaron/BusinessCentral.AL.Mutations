using AlMutate.Models;
using AlMutate.Services;
using Xunit;

namespace AlMutate.Tests;

public class MutationLogTests : IDisposable
{
    private readonly List<string> _tempFiles = new();

    private string TempFile()
    {
        var path = Path.GetTempFileName();
        _tempFiles.Add(path);
        return path;
    }

    public void Dispose()
    {
        foreach (var f in _tempFiles)
        {
            if (File.Exists(f))
                File.Delete(f);
        }
    }

    // -----------------------------------------------------------------------
    // Create_EmptyLog_HasNoRuns
    // -----------------------------------------------------------------------
    [Fact]
    public void Create_EmptyLog_HasNoRuns()
    {
        var path = TempFile();
        var log = MutationLog.Create(path, "./src");

        Assert.Empty(log.GetSurvivedFromLastRun());
        Assert.Equal("M001", log.NextMutationId());
    }

    // -----------------------------------------------------------------------
    // Load_ExistingLog_ParsesCorrectly
    // -----------------------------------------------------------------------
    [Fact]
    public void Load_ExistingLog_ParsesCorrectly()
    {
        var json = """
            {
              "schema_version": 1,
              "project": "./src",
              "runs": [
                {
                  "run": 1,
                  "date": "2026-04-10T09:00:00",
                  "mutations": [
                    {
                      "id": "M001",
                      "operator": "rel-gt-to-gte",
                      "file": "src/CreditManagement.al",
                      "line": 42,
                      "original": "if Amount > 0 then",
                      "mutated": "if Amount >= 0 then",
                      "status": "KILLED",
                      "caught_by": null
                    },
                    {
                      "id": "M002",
                      "operator": "stmt-remove-error",
                      "file": "src/CreditManagement.al",
                      "line": 87,
                      "original": "Error('Credit limit exceeded.');",
                      "mutated": "// Error('Credit limit exceeded.');",
                      "status": "SURVIVED",
                      "caught_by": null
                    }
                  ]
                }
              ]
            }
            """;

        var path = TempFile();
        File.WriteAllText(path, json);

        var log = MutationLog.Load(path);
        var survived = log.GetSurvivedFromLastRun();

        Assert.Single(survived);
        Assert.Equal("M002", survived[0].Id);
        Assert.Equal(MutationStatus.Survived, survived[0].Status);
        // NextMutationId should be M003 (2 results already in log)
        Assert.Equal("M003", log.NextMutationId());
    }

    // -----------------------------------------------------------------------
    // AppendRun_IncrementsRunNumber
    // -----------------------------------------------------------------------
    [Fact]
    public void AppendRun_IncrementsRunNumber()
    {
        var path = TempFile();
        var log = MutationLog.Create(path, "./src");

        var result1 = new MutationResult("M001", "rel-gt-to-gte", "src/Foo.al", 10,
            "if x > 0 then", "if x >= 0 then", MutationStatus.Killed, null);

        log.AppendRun(new List<MutationResult> { result1 });
        log.Save();

        // Reload and append again
        var log2 = MutationLog.Load(path);
        var result2 = new MutationResult("M002", "rel-gt-to-gte", "src/Foo.al", 20,
            "if x < 0 then", "if x <= 0 then", MutationStatus.Survived, null);
        log2.AppendRun(new List<MutationResult> { result2 });
        log2.Save();

        var log3 = MutationLog.Load(path);
        var survived = log3.GetSurvivedFromLastRun();

        // Last run (run 2) has one survived
        Assert.Single(survived);
        Assert.Equal("M002", survived[0].Id);
        // NextMutationId should be M003
        Assert.Equal("M003", log3.NextMutationId());
    }

    // -----------------------------------------------------------------------
    // Save_ThenReload_RoundTrips
    // -----------------------------------------------------------------------
    [Fact]
    public void Save_ThenReload_RoundTrips()
    {
        var path = TempFile();
        var log = MutationLog.Create(path, "./myproject");

        var results = new List<MutationResult>
        {
            new("M001", "rel-gt-to-gte", "src/A.al", 5,
                "if x > 0 then", "if x >= 0 then", MutationStatus.Killed, "TestFoo"),
            new("M002", "stmt-remove-error", "src/B.al", 15,
                "Error('msg');", "// Error('msg');", MutationStatus.Survived, null)
        };

        log.AppendRun(results);
        log.Save();

        var loaded = MutationLog.Load(path);
        var survived = loaded.GetSurvivedFromLastRun();

        Assert.Single(survived);
        Assert.Equal("M002", survived[0].Id);
        Assert.Equal(MutationStatus.Survived, survived[0].Status);
        Assert.Equal("src/B.al", survived[0].File);
        Assert.Equal(15, survived[0].Line);
        Assert.Equal("M003", loaded.NextMutationId());
    }

    // -----------------------------------------------------------------------
    // GetSurvivedFromLastRun_FiltersSurvived
    // -----------------------------------------------------------------------
    [Fact]
    public void GetSurvivedFromLastRun_FiltersSurvived()
    {
        var path = TempFile();
        var log = MutationLog.Create(path, "./src");

        var results = new List<MutationResult>
        {
            new("M001", "op1", "f.al", 1, "orig", "mut", MutationStatus.Killed, "T1"),
            new("M002", "op2", "f.al", 2, "orig", "mut", MutationStatus.Survived, null),
            new("M003", "op3", "f.al", 3, "orig", "mut", MutationStatus.CompileError, null),
            new("M004", "op4", "f.al", 4, "orig", "mut", MutationStatus.Obsolete, null),
        };

        log.AppendRun(results);

        var survived = log.GetSurvivedFromLastRun();

        Assert.Single(survived);
        Assert.Equal("M002", survived[0].Id);
    }

    // -----------------------------------------------------------------------
    // GetSurvivedFromLastRun_EmptyLog_ReturnsEmpty
    // -----------------------------------------------------------------------
    [Fact]
    public void GetSurvivedFromLastRun_EmptyLog_ReturnsEmpty()
    {
        var path = TempFile();
        var log = MutationLog.Create(path, "./src");

        var survived = log.GetSurvivedFromLastRun();

        Assert.Empty(survived);
    }

    // -----------------------------------------------------------------------
    // NextMutationId_EmptyLog_ReturnsM001
    // -----------------------------------------------------------------------
    [Fact]
    public void NextMutationId_EmptyLog_ReturnsM001()
    {
        var path = TempFile();
        var log = MutationLog.Create(path, "./src");

        Assert.Equal("M001", log.NextMutationId());
    }

    // -----------------------------------------------------------------------
    // NextMutationId_AfterAppend_Increments
    // -----------------------------------------------------------------------
    [Fact]
    public void NextMutationId_AfterAppend_Increments()
    {
        var path = TempFile();
        var log = MutationLog.Create(path, "./src");

        var results = new List<MutationResult>
        {
            new("M001", "op1", "f.al", 1, "orig", "mut", MutationStatus.Killed, "T1"),
        };

        log.AppendRun(results);

        Assert.Equal("M002", log.NextMutationId());
    }
}
