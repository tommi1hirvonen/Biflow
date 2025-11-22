namespace Biflow.Ui.Components.Shared.Executions;

public partial class ExecutionDependenciesGraph(IDbContextFactory<AppDbContext> dbContextFactory) : ComponentBase
{
    [Parameter, EditorRequired] public Guid? ExecutionId { get; set; }
    
    [Parameter, EditorRequired] public ExecutionMode? ExecMode { get; set; }
    
    [Parameter, EditorRequired] public StepExecution[]? StepExecutions { get; set; }

    [Parameter, EditorRequired] public Action<StepExecution[]> OnStepExecutionsUpdated { get; set; } = _ => { };

    [Parameter] public Guid? InitialStepId { get; set; }
    
    private StepExecution[]? _stepExecutions;
    private bool _loading;
    private DependencyGraph<StepExecution>? _dependencyGraph;
    private StepExecution? _dependencyGraphStepFilter;
    private StepExecutionDetailsOffcanvas? _stepExecutionDetailsOffcanvas;
    private StepHistoryOffcanvas? _stepHistoryOffcanvas;
    private DependencyGraphDirection _direction = DependencyGraphDirection.LeftToRight;
    private bool _initialRender = true; // true until the graph has been rendered for the first time

    private int FilterDepthBackwards
    {
        get;
        set => field = value >= 0 ? value : field;
    }

    private int FilterDepthForwards
    {
        get;
        set => field = value >= 0 ? value : field;
    }

    private StepExecution? ItemFromNodeId(string nodeId)
    {
        return nodeId.Split('_') switch
        {
            [var item1, var item2] when
                Guid.TryParse(item1, out var execId)
                && Guid.TryParse(item2, out var stepId) =>
                    _stepExecutions?.FirstOrDefault(e => e.ExecutionId == execId && e.StepId == stepId),
            _ => null
        };
    }

    private Task SetDirectionAsync(DependencyGraphDirection direction)
    {
        if (_direction == direction)
        {
            return Task.CompletedTask;
        }
        _direction = direction;
        return LoadGraphAsync();
    }

    public async Task LoadDataAndGraphAsync(bool forceReload = false, CancellationToken cancellationToken = default)
    {
        if (_loading)
        {
            return;
        }
        
        _loading = true;

        if (StepExecutions is null || forceReload)
        {
            await using var dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken);
            var steps = await dbContext.StepExecutions
                .Where(e => e.ExecutionId == ExecutionId)
                .Include(e => e.ExecutionDependencies)
                .Include(e => e.StepExecutionAttempts)
                .Include(e => e.MonitoredStepExecutions)
                .ThenInclude(e => e.MonitoredStepExecution)
                .ThenInclude(e => e.StepExecutionAttempts)
                .ToArrayAsync(cancellationToken);
            _stepExecutions = steps
                .Concat(steps
                    .SelectMany(e => e.MonitoredStepExecutions.Where(m =>
                        m.MonitoringReason is MonitoringReason.UpstreamDependency or MonitoringReason.DownstreamDependency))
                    .Select(e => e.MonitoredStepExecution)
                    .Where(e => e.ExecutionId != ExecutionId))
                .ToArray();
            OnStepExecutionsUpdated(_stepExecutions);
        }
        else
        {
            _stepExecutions = StepExecutions;
        }
        
        _loading = false;
        
        await LoadGraphAsync();
    }

    private async Task LoadGraphAsync()
    {
        if (!ExecutionId.HasValue)
        {
            throw new ArgumentNullException(nameof(ExecutionId));
        }
        ArgumentNullException.ThrowIfNull(_stepExecutions);

        // Use the InitialStepId parameter to filter the graph only on the initial render.
        // Subsequent renders are filtered by the _dependencyGraphStepFilter field set by the autosuggest field.
        if (_initialRender)
        {
            _initialRender = false;
            if (InitialStepId is { } id)
                _dependencyGraphStepFilter = _stepExecutions.FirstOrDefault(s => s.StepId == id);
        }
        
        var allNodes = _stepExecutions
            .Select(step =>
            {
                var status = step.ExecutionStatus.ToString() ?? "";
                var @internal = step.ExecutionId == ExecutionId;
                return new DependencyGraphNode(
                    Id: $"{step.ExecutionId}_{step.StepId}",
                    Name: step.StepName,
                    CssClass: $"enabled {status.ToLower()} {(@internal ? "internal" : "external")}",
                    TooltipText: $"{step.StepType}, {status}, {step.GetDurationInSeconds().SecondsToReadableFormat()}",
                    EnableOnClick: true
                );
            })
            .ToArray();
        var crossExecutionEdgesUpstream = _stepExecutions
                .SelectMany(e => e.MonitoredStepExecutions.Where(m => m.MonitoringReason == MonitoringReason.UpstreamDependency))
                .Where(m => m.MonitoredExecutionId != ExecutionId)
                .Select(m =>
                {
                    var dependencyType = m.StepExecution.ExecutionDependencies
                        .FirstOrDefault(d => d.DependantOnStepId == m.MonitoredStepId)
                        ?.DependencyType
                        ?? DependencyType.OnCompleted;
                    return new DependencyGraphEdge(
                        Id: $"{m.ExecutionId}_{m.StepId}",
                        DependsOnId: $"{m.MonitoredExecutionId}_{m.MonitoredStepId}",
                        CssClass: dependencyType.ToString().ToLower());
                });
        var crossExecutionEdgesDownstream = _stepExecutions
            .SelectMany(e => e.MonitoredStepExecutions.Where(m => m.MonitoringReason == MonitoringReason.DownstreamDependency))
            .Where(m => m.MonitoredExecutionId != ExecutionId)
            .Select(m => new DependencyGraphEdge(
                Id: $"{m.MonitoredExecutionId}_{m.MonitoredStepId}",
                DependsOnId: $"{m.ExecutionId}_{m.StepId}",
                CssClass: nameof(DependencyType.OnCompleted).ToLower()));
        var allEdges = _stepExecutions
            .SelectMany(step => step.ExecutionDependencies)
            .Where(dep => _stepExecutions.Any(s => dep.DependantOnStepId == s.StepId))
            .Select(dep => new DependencyGraphEdge(
                Id: $"{dep.ExecutionId}_{dep.StepId}",
                DependsOnId: $"{dep.ExecutionId}_{dep.DependantOnStepId}",
                CssClass: dep.DependencyType.ToString().ToLower()
            ))
            .Concat(crossExecutionEdgesUpstream)
            .Concat(crossExecutionEdgesDownstream)
            .ToArray();

        DependencyGraphNode[] nodes;
        DependencyGraphEdge[] edges;
        if (_dependencyGraphStepFilter is null)
        {
            // Create a list of steps and dependencies and send them through JSInterop as JSON objects.
            nodes = allNodes;
            edges = allEdges;
        }
        else
        {
            var startNode = allNodes.FirstOrDefault(n => n.Id == $"{_dependencyGraphStepFilter.ExecutionId}_{_dependencyGraphStepFilter.StepId}");
            if (startNode is not null)
            {
                var recursedNodes = RecurseDependenciesBackward(allNodes, startNode, [], allEdges, 0);
                recursedNodes.Remove(startNode);
                recursedNodes = RecurseDependenciesForward(allNodes, startNode, recursedNodes, allEdges, 0);
                nodes = [.. recursedNodes];
                edges = allEdges.Where(e => nodes.Any(n => n.Id == e.Id) && nodes.Any(n => n.Id == e.DependsOnId)).ToArray();
            }
            else
            {
                return;
            }
        }

        if (_dependencyGraph is not null)
        {
            await _dependencyGraph.DrawAsync(nodes, edges, _direction);
        }
        await InvokeAsync(StateHasChanged);
    }

    private List<DependencyGraphNode> RecurseDependenciesBackward(
        IEnumerable<DependencyGraphNode> allNodes,
        DependencyGraphNode node,
        List<DependencyGraphNode> processedNodes,
        IEnumerable<DependencyGraphEdge> edges,
        int depth)
    {
        // If the step was already handled, return.
        // This way we do not loop indefinitely in case of circular dependencies.
        if (processedNodes.Any(n => n.Id == node.Id))
        {
            return processedNodes;
        }

        if (depth++ > FilterDepthBackwards && FilterDepthBackwards > 0)
        {
            return processedNodes;
        }

        processedNodes.Add(node);

        // Get dependency steps.
        var dependencyNodes = allNodes
            .Where(n => edges.Any(e => e.Id == node.Id && e.DependsOnId == n.Id))
            .ToList();

        // Loop through the dependencies and handle them recursively.
        foreach (var dependencyNode in dependencyNodes)
        {
            RecurseDependenciesBackward(allNodes, dependencyNode, processedNodes, edges, depth);
        }

        return processedNodes;
    }

    private List<DependencyGraphNode> RecurseDependenciesForward(
        IEnumerable<DependencyGraphNode> allNodes,
        DependencyGraphNode node,
        List<DependencyGraphNode> processedNodes,
        IEnumerable<DependencyGraphEdge> edges,
        int depth)
    {
        if (processedNodes.Any(n => n.Id == node.Id))
        {
            return processedNodes;
        }

        if (depth++ > FilterDepthForwards && FilterDepthForwards > 0)
        {
            return processedNodes;
        }

        processedNodes.Add(node);

        var dependencyNodes = allNodes
            .Where(n => edges.Any(e => e.DependsOnId == node.Id && e.Id == n.Id))
            .ToList();

        foreach (var dependencyNode in dependencyNodes)
        {
            RecurseDependenciesForward(allNodes, dependencyNode, processedNodes, edges, depth);
        }

        return processedNodes;
    }

    private async Task ShowStepExecutionOffcanvas(StepExecution step)
    {
        var attempt = step.StepExecutionAttempts.OrderByDescending(s => s.StartedOn).First();
        StateHasChanged();
        await _stepExecutionDetailsOffcanvas.LetAsync(x => x.ShowAsync(attempt));
    }

    private Task<AutosuggestDataProviderResult<StepExecution>> ProvideSuggestions(AutosuggestDataProviderRequest request)
    {
        ArgumentNullException.ThrowIfNull(_stepExecutions);
        var filtered = _stepExecutions.Where(s => s.StepName.ContainsIgnoreCase(request.UserInput));
        return Task.FromResult(new AutosuggestDataProviderResult<StepExecution>
        {
            Data = filtered
        });
    }

    private static string TextSelector(StepExecution step) => step.StepName;
}
