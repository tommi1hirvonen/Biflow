using Biflow.DataAccess.Models;

namespace Biflow.Ui.Core;

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
