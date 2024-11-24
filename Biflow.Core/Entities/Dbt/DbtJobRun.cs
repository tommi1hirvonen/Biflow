namespace Biflow.Core.Entities;

public record DbtJobRun(
    long Id,
    DbtJobRunStatus Status,
    string StatusHumanized,
    string? StatusMessage,
    string? GitBranch,
    string? GitSha);