using Biflow.DataAccess.Models;

namespace Biflow.DataAccess;

/// <summary>
/// Convenience class inheriting TopologicalComparer<Step, Guid>.
/// Used to compare/sort steps based on their dependencies.
/// </summary>
public class TopologicalStepExecutionComparer(IEnumerable<StepExecution> steps) : TopologicalComparer<StepExecution, Guid>(
        steps,
        step => step?.StepId ?? Guid.Empty,
        step => step.ExecutionDependencies.Select(d => d.DependantOnStepId ?? Guid.Empty));