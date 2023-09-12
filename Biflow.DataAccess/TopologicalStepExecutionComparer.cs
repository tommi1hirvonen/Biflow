using Biflow.DataAccess.Models;

namespace Biflow.DataAccess;

/// <summary>
/// Convenience class inheriting TopologicalComparer<Step, Guid>.
/// Used to compare/sort steps based on their dependencies.
/// </summary>
public class TopologicalStepExecutionComparer : TopologicalComparer<StepExecution, Guid>
{
    public TopologicalStepExecutionComparer(IEnumerable<StepExecution> steps)
        : base(
            steps,
            step => step?.StepId ?? Guid.Empty,
            step => step.ExecutionDependencies.Select(d => d.DependantOnStepId))
    {

    }
}
