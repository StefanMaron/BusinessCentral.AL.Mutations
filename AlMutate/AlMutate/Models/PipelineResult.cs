namespace AlMutate.Models;

public record PipelineResult(
    int ExitCode,
    List<MutationResult> Results,
    double Score,
    string? ReportMarkdown,
    string? ErrorMessage = null);
