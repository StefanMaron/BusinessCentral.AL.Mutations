using System.Diagnostics;

namespace AlMutate.Services;

public static class GitService
{
    /// <summary>
    /// Returns true if the git working tree rooted at <paramref name="workingDirectory"/>
    /// is clean (i.e. <c>git status --porcelain</c> produces no output).
    /// </summary>
    public static bool IsWorkingTreeClean(string? workingDirectory = null)
    {
        var (exitCode, stdout, _) = RunGit("status --porcelain", workingDirectory);
        if (exitCode != 0) return false;
        // Ignore untracked files (??) — we only mutate tracked files
        var trackedChanges = stdout
            .Split('\n', StringSplitOptions.RemoveEmptyEntries)
            .Where(line => !line.StartsWith("??"));
        return !trackedChanges.Any();
    }

    /// <summary>
    /// Runs <c>git checkout -- &lt;filePath&gt;</c> to restore a file to its committed state.
    /// </summary>
    /// <exception cref="MutationException">
    /// Thrown if the git command fails or git is not available.
    /// </exception>
    public static void RestoreFile(string filePath)
    {
        var workingDir = Path.GetDirectoryName(Path.GetFullPath(filePath));
        var (exitCode, _, stderr) = RunGit($"checkout -- \"{filePath}\"", workingDir);

        if (exitCode != 0)
            throw new MutationException(
                $"Failed to restore '{filePath}' via git checkout: {stderr.Trim()}");
    }

    // -----------------------------------------------------------------------
    // Helpers

    private static (int ExitCode, string Stdout, string Stderr) RunGit(
        string arguments,
        string? workingDirectory)
    {
        try
        {
            var psi = new ProcessStartInfo("git", arguments)
            {
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
            };

            if (!string.IsNullOrEmpty(workingDirectory))
                psi.WorkingDirectory = workingDirectory;

            using var proc = Process.Start(psi)
                ?? throw new MutationException("Failed to start git process.");

            var stdout = proc.StandardOutput.ReadToEnd();
            var stderr = proc.StandardError.ReadToEnd();
            proc.WaitForExit();

            return (proc.ExitCode, stdout, stderr);
        }
        catch (MutationException)
        {
            throw;
        }
        catch (Exception ex)
        {
            throw new MutationException($"git is not available or could not be executed: {ex.Message}");
        }
    }
}
