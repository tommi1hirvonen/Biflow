using Biflow.DataAccess.Models;

namespace Biflow.DataAccess;

/// <summary>
/// Convenience class inheriting TopologicalComparer<Step, Guid>.
/// Used to compare/sort steps based on their dependencies.
/// </summary>
public class TopologicalStepComparer : TopologicalComparer<Step, Guid>
{
    public TopologicalStepComparer(IEnumerable<Step> steps)
        : base(
            steps,
            step => step?.StepId ?? Guid.Empty,
            step => step.Dependencies.Select(d => d.DependantOnStepId))
    {

    }
}
