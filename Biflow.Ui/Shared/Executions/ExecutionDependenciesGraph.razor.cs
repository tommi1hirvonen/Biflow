﻿namespace Biflow.Ui.Shared.Executions;

public partial class ExecutionDependenciesGraph : ComponentBase
{
    [Parameter] public Execution? Execution { get; set; }

    [Parameter] public Guid? InitialStepId { get; set; }

    private DependencyGraph<StepExecution>? _dependencyGraph;
    private StepExecution? _dependencyGraphStepFilter;
    private StepExecutionDetailsOffcanvas? _stepExecutionDetailsOffcanvas;
    private StepHistoryOffcanvas? _stepHistoryOffcanvas;
    private DependencyGraphDirection _direction = DependencyGraphDirection.LeftToRight;

    private int FilterDepthBackwards
    {
        get => _filterDepthBackwards;
        set => _filterDepthBackwards = value >= 0 ? value : _filterDepthBackwards;
    }

    // TODO Replace with field keyword in .NET 10
    private int _filterDepthBackwards;

    private int FilterDepthForwards
    {
        get => _filterDepthForwards;
        set => _filterDepthForwards = value >= 0 ? value : _filterDepthForwards;
    }

    // TODO Replace with field keyword in .NET 10
    private int _filterDepthForwards;

    private IEnumerable<StepExecution>? StepExecutions => Execution?.StepExecutions
        .Concat(Execution.StepExecutions
            .SelectMany(e => e.MonitoredStepExecutions.Where(m => m.MonitoringReason is MonitoringReason.UpstreamDependency or MonitoringReason.DownstreamDependency))
            .Select(e => e.MonitoredStepExecution)
            .Where(e => e.ExecutionId != Execution.ExecutionId));

    private StepExecution? ItemFromNodeId(string nodeId)
    {
        return nodeId.Split('_') switch
        {
            [var item1, var item2] when
                Guid.TryParse(item1, out var execId)
                && Guid.TryParse(item2, out var stepId) =>
                    StepExecutions?.FirstOrDefault(e => e.ExecutionId == execId && e.StepId == stepId),
            _ => null
        };
    }

    protected override void OnAfterRender(bool firstRender)
    {
        if (!firstRender) return;
        if (InitialStepId is not { } filterStepId) return;
        _dependencyGraphStepFilter = Execution?.StepExecutions.FirstOrDefault(s => s.StepId == filterStepId);
        StateHasChanged();
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

    public async Task LoadGraphAsync()
    {
        ArgumentNullException.ThrowIfNull(_dependencyGraph);
        ArgumentNullException.ThrowIfNull(Execution);
        ArgumentNullException.ThrowIfNull(StepExecutions);

        var allNodes = StepExecutions
            .Select(step =>
            {
                var status = step.ExecutionStatus.ToString() ?? "";
                var @internal = step.ExecutionId == Execution.ExecutionId;
                return new DependencyGraphNode(
                    Id: $"{step.ExecutionId}_{step.StepId}",
                    Name: step.StepName,
                    CssClass: $"enabled {status.ToLower()} {(@internal ? "internal" : "external")}",
                    TooltipText: $"{step.StepType}, {status}, {step.GetDurationInSeconds().SecondsToReadableFormat()}",
                    EnableOnClick: true
                );
            })
            .ToArray();
        var crossExecutionEdgesUpstream = Execution.StepExecutions
                .SelectMany(e => e.MonitoredStepExecutions.Where(m => m.MonitoringReason == MonitoringReason.UpstreamDependency))
                .Where(m => m.MonitoredExecutionId != Execution.ExecutionId)
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
        var crossExecutionEdgesDownstream = Execution.StepExecutions
            .SelectMany(e => e.MonitoredStepExecutions.Where(m => m.MonitoringReason == MonitoringReason.DownstreamDependency))
            .Where(m => m.MonitoredExecutionId != Execution.ExecutionId)
            .Select(m => new DependencyGraphEdge(
                Id: $"{m.MonitoredExecutionId}_{m.MonitoredStepId}",
                DependsOnId: $"{m.ExecutionId}_{m.StepId}",
                CssClass: DependencyType.OnCompleted.ToString().ToLower()));
        var allEdges = Execution.StepExecutions
            .SelectMany(step => step.ExecutionDependencies)
            .Where(dep => Execution.StepExecutions.Any(s => dep.DependantOnStepId == s.StepId))
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
        await _dependencyGraph.DrawAsync(nodes, edges, _direction);
        StateHasChanged();
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
        var attempt = step?.StepExecutionAttempts.OrderByDescending(s => s.StartedOn).First();
        if (attempt is null)
        {
            return;
        }
        StateHasChanged();
        await _stepExecutionDetailsOffcanvas.LetAsync(x => x.ShowAsync(attempt));
    }

    private Task<AutosuggestDataProviderResult<StepExecution>> ProvideSuggestions(AutosuggestDataProviderRequest request)
    {
        ArgumentNullException.ThrowIfNull(StepExecutions);
        var filtered = StepExecutions.Where(s => s.StepName.ContainsIgnoreCase(request.UserInput));
        return Task.FromResult(new AutosuggestDataProviderResult<StepExecution>
        {
            Data = filtered
        });
    }

    private static string TextSelector(StepExecution step) => step.StepName;
}
