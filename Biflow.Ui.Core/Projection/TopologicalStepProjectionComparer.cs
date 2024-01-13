using Biflow.Core;

namespace Biflow.Ui.Core.Projection;

/// <summary>
/// Convenience class inheriting TopologicalComparer<Step, Guid>.
/// Used to compare/sort steps based on their dependencies.
/// </summary>
public class TopologicalStepProjectionComparer(IEnumerable<StepProjection> steps) : TopologicalComparer<StepProjection, Guid>(
        steps,
        step => step?.StepId ?? Guid.Empty,
        step => step.Dependencies);