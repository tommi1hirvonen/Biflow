using OneOf;

namespace Biflow.Executor.Core.Orchestrator;

/// <summary>
/// The orchestration observer deals in four different possible actions for a step: wait, execute, cancel or fail.
/// These are represented by the <see cref="ObserverAction"/> type.
/// </summary>
[GenerateOneOf]
internal partial class ObserverAction : OneOfBase<WaitAction, ExecuteAction, CancelAction, FailAction>;

/// <summary>
/// The orchestrator deals in three different possible actions for a step: execute, cancel or fail.
/// These are represented by the <see cref="OrchestratorAction"/> type.
/// </summary>
[GenerateOneOf]
internal partial class OrchestratorAction : OneOfBase<ExecuteAction, CancelAction, FailAction>;

/// <summary>
/// Common static singleton instances to reduce allocations in the hot path of orchestration, observer and trackers.
/// </summary>
internal static class Actions
{
    public static readonly WaitAction Wait = new();

    public static readonly ExecuteAction Execute = new();

    public static readonly CancelAction Cancel = new();

    internal static FailAction Fail(StepExecutionStatus withStatus, string? errorMessage) => new(withStatus, errorMessage);

    internal static FailAction Fail(StepExecutionStatus withStatus) => new(withStatus, null);
}

internal class WaitAction;

internal class ExecuteAction;

internal class CancelAction;

internal record FailAction(StepExecutionStatus WithStatus, string? ErrorMessage);