namespace AlMutate.Models;

public record MutationCandidate(
    string File,
    int Line,
    string OperatorId,
    string Original,
    string Mutated);
