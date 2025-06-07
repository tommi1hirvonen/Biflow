using Biflow.Core;

namespace Biflow.Ui.Core.Projection;

/// <summary>
/// Convenience class inheriting <see cref="TopologicalComparer{TItem,TKey}"/>.
/// Used to compare/sort steps based on their dependencies.
/// </summary>
public class TopologicalStepProjectionComparer(IEnumerable<StepProjection> steps) : TopologicalComparer<StepProjection, Guid>(
        steps,
        step => step?.StepId ?? Guid.Empty,
        step => step.Dependencies.Select(d => d.DependentOnStepId));