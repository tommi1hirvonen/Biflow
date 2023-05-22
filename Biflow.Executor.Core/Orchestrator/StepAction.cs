namespace Biflow.Executor.Core.Orchestrator;

internal enum StepAction
{
    Wait,
    Execute,
    FailDuplicate,
    FailDependencies,
    Cancel,
    FailFirstError
}
