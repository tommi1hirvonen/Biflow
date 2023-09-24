using Biflow.DataAccess;
using Biflow.DataAccess.Models;

namespace Biflow.Ui.Core.Projection;

/// <summary>
/// Convenience class inheriting TopologicalComparer<Step, Guid>.
/// Used to compare/sort steps based on their dependencies.
/// </summary>
public class TopologicalStepProjectionComparer : TopologicalComparer<StepProjection, Guid>
{
    public TopologicalStepProjectionComparer(IEnumerable<StepProjection> steps)
        : base(
            steps,
            step => step?.StepId ?? Guid.Empty,
            step => step.Dependencies)
    {

    }
}
