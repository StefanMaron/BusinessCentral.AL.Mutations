using AlMutate.Models;
using System.Text;

namespace AlMutate.Services;

public static class ReportGenerator
{
    // Score = (Killed / Testable) * 100
    // Testable = Killed + Survived (excludes CompileError and Obsolete)
    // Returns 100.0 when testable == 0
    public static double CalculateScore(List<MutationResult> results)
    {
        var killed = results.Count(r => r.Status == MutationStatus.Killed);
        var survived = results.Count(r => r.Status == MutationStatus.Survived);
        var testable = killed + survived;

        if (testable == 0)
            return 100.0;

        return (double)killed / testable * 100.0;
    }

    // Generate a Markdown report including: score, summary table, list of survivors
    public static string GenerateMarkdown(List<MutationResult> results, string project)
    {
        var score = CalculateScore(results);
        var killed = results.Count(r => r.Status == MutationStatus.Killed);
        var survived = results.Count(r => r.Status == MutationStatus.Survived);
        var compileError = results.Count(r => r.Status == MutationStatus.CompileError);
        var obsolete = results.Count(r => r.Status == MutationStatus.Obsolete);
        var timedOut = results.Count(r => r.Status == MutationStatus.TimedOut);

        var sb = new StringBuilder();
        sb.AppendLine($"# Mutation Testing Report — {project}");
        sb.AppendLine();
        sb.AppendLine($"**Mutation Score: {score.ToString("F2", System.Globalization.CultureInfo.InvariantCulture)}%**");
        sb.AppendLine();
        sb.AppendLine("## Summary");
        sb.AppendLine();
        sb.AppendLine("| Killed | Survived | Compile Error | Timed Out | Obsolete |");
        sb.AppendLine("|--------|----------|---------------|-----------|----------|");
        sb.AppendLine($"| {killed} | {survived} | {compileError} | {timedOut} | {obsolete} |");
        sb.AppendLine();
        sb.AppendLine("## Survivors");
        sb.AppendLine();

        var survivors = results.Where(r => r.Status == MutationStatus.Survived).ToList();
        if (survivors.Count == 0)
        {
            sb.AppendLine("No survivors — all testable mutations were killed.");
        }
        else
        {
            foreach (var m in survivors)
            {
                sb.AppendLine($"### {m.Id} — `{m.Operator}`");
                sb.AppendLine();
                sb.AppendLine($"- **File:** {m.File}");
                sb.AppendLine($"- **Line:** {m.Line}");
                sb.AppendLine($"- **Original:** `{m.Original}`");
                sb.AppendLine($"- **Mutated:** `{m.Mutated}`");
                sb.AppendLine();
            }
        }

        return sb.ToString();
    }
}
