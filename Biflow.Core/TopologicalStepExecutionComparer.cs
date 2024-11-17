using Biflow.Core.Entities;

namespace Biflow.Core;

/// <summary>
/// Convenience class inheriting <see cref="TopologicalComparer{TItem,TKey}"/>.
/// Used to compare/sort steps based on their dependencies.
/// </summary>
public class TopologicalStepExecutionComparer(IEnumerable<StepExecution> steps) : TopologicalComparer<StepExecution, Guid>(
        steps,
        step => step?.StepId ?? Guid.Empty,
        step => step.ExecutionDependencies.Select(d => d.DependantOnStepId),
        Comparer<StepExecution>.Create((x, y) => x.ExecutionPhase.CompareTo(y.ExecutionPhase)));