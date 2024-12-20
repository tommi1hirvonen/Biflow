using JetBrains.Annotations;

namespace Biflow.Core.Entities;

[PublicAPI]
public record DbtJobRun(
    long Id,
    DbtJobRunStatus Status,
    string StatusHumanized,
    string? StatusMessage,
    string? GitBranch,
    string? GitSha);