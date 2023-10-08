using Biflow.DataAccess.Models;

namespace Biflow.DataAccess;

/// <summary>
/// Convenience class inheriting TopologicalComparer<Step, Guid>.
/// Used to compare/sort steps based on their dependencies.
/// </summary>
public class TopologicalStepComparer(IEnumerable<Step> steps) : TopologicalComparer<Step, Guid>(
        steps,
        step => step?.StepId ?? Guid.Empty,
        step => step.Dependencies.Select(d => d.DependantOnStepId));