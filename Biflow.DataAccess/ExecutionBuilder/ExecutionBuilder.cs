namespace Biflow.DataAccess;

public partial class ExecutionBuilder : IDisposable
{
    private readonly AppDbContext _context;
    private readonly Step[] _steps;
    private readonly ExecutionBuilderStep[] _builderSteps;
    private readonly Func<Execution> _createExecution;
    private Execution _execution;

    internal ExecutionBuilder(AppDbContext context, Func<Execution> createExecution, Step[] steps)
    {
        _context = context;
        _createExecution = createExecution;
        _execution = _createExecution();

        // Guard agains errors in TopologicalStepComparer when encountering cyclic dependencies.
        if (_execution.ExecutionMode == ExecutionMode.Dependency)
        {
            try
            {
                _steps = [.. steps.OrderBy(s => s, new TopologicalStepComparer(steps))];
            }
            catch
            {
                _steps = [.. steps.OrderBy(s => s.ExecutionPhase)];
            }
        }
        else
        {
            _steps = [.. steps.OrderBy(s => s.ExecutionPhase)];
        }

        _builderSteps = _steps
            .Select(s => new ExecutionBuilderStep(this, s))
            .ToArray();
    }

    public ExecutionMode ExecutionMode => _execution.ExecutionMode;

    public bool Notify { get => _execution.Notify; set => _execution.Notify = value; }

    public AlertType? NotifyCaller { get => _execution.NotifyCaller; set => _execution.NotifyCaller = value; }

    public bool NotifyCallerOvertime { get => _execution.NotifyCallerOvertime; set => _execution.NotifyCallerOvertime = value; }

    public double TimeoutMinutes { get => _execution.TimeoutMinutes; set => _execution.TimeoutMinutes = value; }

    public IEnumerable<ExecutionBuilderStep> Steps =>
        _builderSteps.Where(s => !_execution.StepExecutions.Any(e => s.StepId == e.StepId));

    public IEnumerable<ExecutionBuilderStepExecution> StepExecutions
    {
        get
        {
            // Guard agains errors in TopologicalStepExecutionComparer when encountering cyclic dependencies.
            IEnumerable<StepExecution> steps;
            if (_execution.ExecutionMode == ExecutionMode.Dependency)
            {
                try
                {
                    steps = _execution.StepExecutions
                        .OrderBy(e => e, new TopologicalStepExecutionComparer(_execution.StepExecutions));
                }
                catch
                {
                    steps = _execution.StepExecutions
                        .OrderBy(e => e.ExecutionPhase);
                }
            }
            else
            {
                steps = _execution.StepExecutions
                    .OrderBy(e => e.ExecutionPhase);
            }

            return steps.Select(e => new ExecutionBuilderStepExecution(this, e));
        }
    }

    public IEnumerable<DynamicParameter> Parameters => _execution.ExecutionParameters;

    /// <summary>
    /// 
    /// </summary>
    /// <returns><see cref="Execution"/> that was saved to the database.
    /// <see langword="null"/> if no <see cref="StepExecution"/> objects were included.</returns>
    public async Task<Execution?> SaveExecutionAsync(CancellationToken cancellationToken = default)
    {
        if (_execution.StepExecutions.Count == 0)
        {
            return null;
        }
        _context.Executions.Add(_execution);
        await _context.SaveChangesAsync(cancellationToken);
        return _execution;
    }

    /// <summary>
    /// Resets the builder by creating a new <see cref="Execution"/> placeholder with no <see cref="StepExecution"/>s
    /// </summary>
    public void Reset() => _execution = _createExecution();

    /// <summary>
    /// Clears/removes all <see cref="StepExecution"/> objects from the <see cref="Execution"/> object
    /// </summary>
    public void Clear() => _execution.StepExecutions.Clear();

    public void AddAll(Func<ExecutionBuilderStep, bool>? predicate = null)
    {
        predicate ??= (_) => true;
        foreach (var step in Steps.Where(s => predicate(s)))
        {
            step.AddToExecution();
        }
    }

    internal bool Add(Step step)
    {
        // Step was already added.
        if (_execution.StepExecutions.Any(e => e.StepId == step.StepId))
        {
            return false;
        }
        // Step is not one of the provided steps.
        if (!_steps.Contains(step))
        {
            throw new ArgumentException($"Argument {nameof(step)} value was not in the list of provided steps");
        }
        var stepExecution = step.ToStepExecution(_execution);
        _execution.StepExecutions.Add(stepExecution);
        return true;
    }

    internal bool Remove(StepExecution stepExecution) =>
        _execution.StepExecutions.Remove(stepExecution);

    internal void AddWithDependencies(Step step, bool onlyOnSuccess) =>
        RecurseDependencies(step, [], onlyOnSuccess);

    private void RecurseDependencies(Step step, List<Step> processedSteps, bool onlyOnSuccess)
    {
        // Add the step to the list of steps to execute if it is not there yet.
        Add(step);

        // Get dependency ids.
        List<Guid> dependencyStepIds = step.Dependencies
            .Where(d => d.DependencyType == DependencyType.OnSucceeded || !onlyOnSuccess)
            .Select(d => d.DependantOnStepId)
            .ToList();

        // If there are no dependencies, return true.
        if (dependencyStepIds.Count == 0)
        {
            return;
        }
        // This step was already handled.
        else if (processedSteps.Any(s => s.StepId == step.StepId))
        {
            return;
        }

        processedSteps.Add(step);

        // Get dependency steps based on ids. Only include enabled steps.
        List<Step> dependencySteps = _steps
            ?.Where(s => s.IsEnabled && dependencyStepIds.Any(id => s.StepId == id))
            .ToList()
            ?? [];

        // Loop through the dependencies and handle them recursively.
        foreach (var depencyStep in dependencySteps)
        {
            RecurseDependencies(depencyStep, processedSteps, onlyOnSuccess);
        }

    }

    public void Dispose() => _context.Dispose();
}