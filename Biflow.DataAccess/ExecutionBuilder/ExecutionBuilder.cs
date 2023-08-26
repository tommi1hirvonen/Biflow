using Biflow.DataAccess.Models;

namespace Biflow.DataAccess;

public class ExecutionBuilder : IDisposable
{
    private readonly BiflowContext _context;

    internal ExecutionBuilder(BiflowContext context, Execution execution, IEnumerable<Step> steps)
    {
        _context = context;
        Execution = execution;
        Steps = steps;
    }

    public Execution Execution { get; }

    public IEnumerable<Step> Steps { get; }

    public bool AddStep(Step step)
    {
        if (Execution.StepExecutions.Any(e => e.StepId == step.StepId))
        {
            return false;
        }
        var stepExecution = step.ToStepExecution(Execution);
        Execution.StepExecutions.Add(stepExecution);
        return true;
    }

    public async Task SaveAsync()
    {
        foreach (var step in Execution.StepExecutions)
        {
            var toDelete = step.ExecutionDependencies
                .Where(d => !Execution.StepExecutions.Any(e => d.DependantOnStepId == e.StepId))
                .ToArray();
            foreach (var dependency in toDelete)
            {
                step.ExecutionDependencies.Remove(dependency);
            }
        }
        _context.Executions.Add(Execution);
        await _context.SaveChangesAsync();
    }

    public void Dispose() => _context.Dispose();
}