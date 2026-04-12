using System.Diagnostics;
using System.IO;
using AlMutate;
using AlMutate.Services;
using Xunit;

namespace AlMutate.Tests;

public class GitServiceTests : IDisposable
{
    private readonly string _tempDir;

    public GitServiceTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), "almutate_git_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_tempDir);
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempDir))
            Directory.Delete(_tempDir, recursive: true);
    }

    // -----------------------------------------------------------------------
    // Helpers

    private static void RunGit(string args, string workDir)
    {
        var psi = new ProcessStartInfo("git", args)
        {
            WorkingDirectory = workDir,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
        };
        using var proc = Process.Start(psi)!;
        proc.WaitForExit();
        if (proc.ExitCode != 0)
        {
            var err = proc.StandardError.ReadToEnd();
            throw new InvalidOperationException($"git {args} failed: {err}");
        }
    }

    private string InitRepoWithFile(string fileName, string content)
    {
        var filePath = Path.Combine(_tempDir, fileName);
        File.WriteAllText(filePath, content);
        RunGit("init", _tempDir);
        RunGit("config user.email test@test.com", _tempDir);
        RunGit("config user.name TestUser", _tempDir);
        RunGit($"add {fileName}", _tempDir);
        RunGit("commit -m \"init\"", _tempDir);
        return filePath;
    }

    // -----------------------------------------------------------------------

    [Fact]
    public void IsWorkingTreeClean_CleanRepo_ReturnsTrue()
    {
        InitRepoWithFile("sample.al", "codeunit 50100 \"Foo\" { }");

        var result = GitService.IsWorkingTreeClean(_tempDir);

        Assert.True(result);
    }

    [Fact]
    public void IsWorkingTreeClean_DirtyRepo_ReturnsFalse()
    {
        var filePath = InitRepoWithFile("sample.al", "codeunit 50100 \"Foo\" { }");
        File.WriteAllText(filePath, "codeunit 50100 \"Foo\" { /* modified */ }");

        var result = GitService.IsWorkingTreeClean(_tempDir);

        Assert.False(result);
    }

    [Fact]
    public void RestoreFile_AfterModification_RestoresContent()
    {
        const string original = "codeunit 50100 \"Foo\" { }";
        var filePath = InitRepoWithFile("sample.al", original);

        // Modify the file
        File.WriteAllText(filePath, "// mutated");

        GitService.RestoreFile(filePath);

        Assert.Equal(original, File.ReadAllText(filePath));
    }

    [Fact]
    public void RestoreFile_NoGitRepo_ThrowsMutationException()
    {
        // _tempDir has no .git — just a plain directory
        var orphanFile = Path.Combine(_tempDir, "orphan.al");
        File.WriteAllText(orphanFile, "// nothing");

        Assert.Throws<MutationException>(() => GitService.RestoreFile(orphanFile));
    }
}
