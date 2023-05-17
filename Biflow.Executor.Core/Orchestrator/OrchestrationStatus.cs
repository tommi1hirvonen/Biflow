namespace Biflow.Executor.Core.Orchestrator;

/// <summary>
/// Internal enum used by the orchestrator to group step execution statuses on a less detailed level.
/// The level of the internal enum is sufficient to keep track of when and if to execute steps.
/// </summary>
internal enum OrchestrationStatus
{
    NotStarted,
    Running,
    Succeeded,
    Failed
}
