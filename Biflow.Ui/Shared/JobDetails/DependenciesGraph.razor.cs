using Biflow.Ui.Shared.Executions;
using Biflow.Ui.Shared.StepEditModal;

namespace Biflow.Ui.Shared.JobDetails;

public partial class DependenciesGraph : ComponentBase
{
    [Inject] private ToasterService Toaster { get; set; } = null!;
    
    [Inject] private IDbContextFactory<AppDbContext> DbFactory { get; set; } = null!;
    
    [Inject] private IHxMessageBoxService Confirmer { get; set; } = null!;
    
    [Inject] private IMediator Mediator { get; set; } = null!;

    [CascadingParameter] public Job? Job { get; set; }

    [CascadingParameter] public List<Step>? Steps { get; set; }

    [CascadingParameter(Name = "SortSteps")] public Action? SortSteps { get; set; }

    [Parameter] public List<ConnectionBase>? SqlConnections { get; set; }

    [Parameter] public List<MsSqlConnection>? MsSqlConnections { get; set; }

    [Parameter] public List<AnalysisServicesConnection>? AsConnections { get; set; }

    [Parameter] public List<PipelineClient>? PipelineClients { get; set; }

    [Parameter] public List<AppRegistration>? AppRegistrations { get; set; }

    [Parameter] public List<FunctionApp>? FunctionApps { get; set; }

    [Parameter] public List<QlikCloudEnvironment>? QlikCloudClients { get; set; }

    [Parameter] public List<DatabricksWorkspace>? DatabricksWorkspaces { get; set; }

    [Parameter] public List<Credential>? Credentials { get; set; }

    [Parameter] public Guid? InitialStepId { get; set; }

    private readonly Dictionary<StepType, IStepEditModal?> stepEditModals = [];
    private readonly HashSet<StepTag> tagsFilterSet = [];

    private FilterDropdownMode tagsFilterMode = FilterDropdownMode.Any;
    private DependencyGraph<StepProjection>? dependencyGraph;
    private StepHistoryOffcanvas? stepHistoryOffcanvas;
    private Guid? stepFilter;
    private DependencyGraphDirection direction = DependencyGraphDirection.LeftToRight;
    private List<StepProjection>? stepSlims;

    private int FilterDepthBackwards
    {
        get => _filterDepthBackwards;
        set => _filterDepthBackwards = value >= 0 ? value : _filterDepthBackwards;
    }

    private int _filterDepthBackwards;

    private int FilterDepthForwards
    {
        get => _filterDepthForwards;
        set => _filterDepthForwards = value >= 0 ? value : _filterDepthForwards;
    }

    private int _filterDepthForwards;

    private IEnumerable<StepTag> Tags => Steps?
        .SelectMany(step => step.Tags)
        .DistinctBy(t => t.TagName)
        .OrderBy(t => t.TagName)
        .AsEnumerable()
        ?? [];

    private Func<StepProjection, bool> TagFilterPredicate => step =>
        (tagsFilterMode is FilterDropdownMode.Any && (tagsFilterSet.Count == 0 || tagsFilterSet.Any(tag => step.Tags.Any(t => t.TagName == tag.TagName))))
        || (tagsFilterMode is FilterDropdownMode.All && tagsFilterSet.All(tag => step.Tags.Any(t => t.TagName == tag.TagName)));

    protected override void OnAfterRender(bool firstRender)
    {
        if (firstRender)
        {
            if (InitialStepId is Guid filterStepId)
            {
                stepFilter = filterStepId;
                StateHasChanged();
            }
        }
    }

    private Task SetDirectionAsync(DependencyGraphDirection direction)
    {
        if (this.direction == direction)
        {
            return Task.CompletedTask;
        }
        this.direction = direction;
        return LoadGraphAsync();
    }

    private async Task LoadGraphAsync()
    {
        ArgumentNullException.ThrowIfNull(Job);
        ArgumentNullException.ThrowIfNull(dependencyGraph);

        using var context = await DbFactory.CreateDbContextAsync();
        stepSlims = await context.Steps
            .AsNoTrackingWithIdentityResolution()
            .IgnoreQueryFilters()
            .Where(s => s.JobId == Job.JobId
                    || context.Dependencies.Any(d => d.DependantOnStepId == s.StepId && d.Step.JobId == Job.JobId)
                    || context.Dependencies.Any(d => d.StepId == s.StepId && d.DependantOnStep.JobId == Job.JobId))
            .Select(s => new StepProjection(
                s.StepId,
                s.JobId,
                s.Job.JobName,
                s.StepName,
                s.StepType,
                s.ExecutionPhase,
                s.IsEnabled,
                s.Tags.ToArray(),
                s.Dependencies.Select(d => new DependencyProjection(d.StepId, d.DependantOnStepId, d.DependencyType)).ToArray()))
            .ToListAsync();

        List<DependencyGraphNode> nodes;
        List<DependencyGraphEdge> edges;
        if (stepFilter is null)
        {
            var steps = stepSlims.Where(TagFilterPredicate).ToArray();
            nodes = steps
                .Where(TagFilterPredicate)
                .Select(step => new DependencyGraphNode(
                    Id: step.StepId.ToString(),
                    Name: step.StepName ?? "",
                    CssClass: $"{(step.IsEnabled ? "enabled" : "disabled")} {(step.JobId != Job.JobId ? "external" : "internal")}",
                    TooltipText: step.JobId == Job.JobId ? $"{step.StepType}" : $"{step.StepType}, {step.JobName}",
                    EnableOnClick: true
                )).ToList();
            edges = steps
                .SelectMany(step => step.Dependencies)
                .Where(d => steps.Any(s => s.StepId == d.DependentOnStepId))
                .Select(dep => new DependencyGraphEdge(
                    Id: dep.StepId.ToString(),
                    DependsOnId: dep.DependentOnStepId.ToString(),
                    CssClass: dep.DependencyType.ToString().ToLower()
                )).ToList();
        }
        else
        {
            var startStep = stepSlims.FirstOrDefault(s => s.StepId == stepFilter);
            if (startStep is not null)
            {
                var steps = RecurseDependenciesBackward(startStep, stepSlims, [], 0);
                steps.Remove(startStep);
                steps = RecurseDependenciesForward(startStep, stepSlims, steps, 0);

                nodes = steps.Select(step => new DependencyGraphNode(
                    Id: step.StepId.ToString(),
                    Name: step.StepName ?? "",
                    CssClass: $"{(step.IsEnabled ? "enabled" : "disabled")} {(step.JobId != Job.JobId ? "external" : "internal")} {(step.StepId == stepFilter ? "selected" : null)}",
                    TooltipText: $"{step.StepType}",
                    EnableOnClick: true
                )).ToList();
                edges = stepSlims
                    .SelectMany(step => step.Dependencies)
                    .Where(d => steps.Any(s => d.DependentOnStepId == s.StepId) && steps.Any(s => d.StepId == s.StepId)) // only include dependencies whose step is included
                    .Select(dep => new DependencyGraphEdge(
                        Id: dep.StepId.ToString(),
                        DependsOnId: dep.DependentOnStepId.ToString(),
                        CssClass: dep.DependencyType.ToString().ToLower()
                    )).ToList();
            }
            else
            {
                return;
            }
        }

        await dependencyGraph.DrawAsync(nodes, edges, direction);
    }

    private List<StepProjection> RecurseDependenciesBackward(StepProjection step, List<StepProjection> allSteps, List<StepProjection> processedSteps, int depth)
    {
        // If the step was already handled, return.
        // This way we do not loop indefinitely in case of circular dependencies.
        if (processedSteps.Any(s => s.StepId == step.StepId))
        {
            return processedSteps;
        }

        if (depth++ > FilterDepthBackwards && FilterDepthBackwards > 0)
        {
            depth--;
            return processedSteps;
        }

        processedSteps.Add(step);

        // Get dependency steps.
        List<StepProjection> dependencySteps = allSteps.Where(s => step.Dependencies.Any(d => d.DependentOnStepId == s.StepId)).ToList();

        // Loop through the dependencies and handle them recursively.
        foreach (var depencyStep in dependencySteps)
        {
            RecurseDependenciesBackward(depencyStep, allSteps, processedSteps, depth);
        }

        depth--;

        return processedSteps;
    }

    private List<StepProjection> RecurseDependenciesForward(StepProjection step, List<StepProjection> allSteps, List<StepProjection> processedSteps, int depth)
    {
        if (processedSteps.Any(s => s.StepId == step.StepId))
        {
            return processedSteps;
        }

        if (depth++ > FilterDepthForwards && FilterDepthForwards > 0)
        {
            depth--;
            return processedSteps;
        }

        processedSteps.Add(step);

        List<StepProjection> dependencySteps = allSteps.Where(s => s.Dependencies.Any(d => d.DependentOnStepId == step.StepId)).ToList();

        foreach (var depencyStep in dependencySteps)
        {
            RecurseDependenciesForward(depencyStep, allSteps, processedSteps, depth);
        }

        depth--;

        return processedSteps;
    }

    private Task OpenStepEditModalAsync(StepProjection step) =>
        stepEditModals[step.StepType].LetAsync(x => x.ShowAsync(step.StepId, StepEditModalView.Dependencies));

    private async Task OnStepSubmit(Step step)
    {
        ArgumentNullException.ThrowIfNull(Steps);

        var index = Steps.FindIndex(s => s.StepId == step.StepId);
        if (index is int i and >= 0)
        {
            Steps.RemoveAt(i);
            Steps.Insert(i, step);
        }
        else
        {
            Steps.Add(step);
        }

        SortSteps?.Invoke();

        await LoadGraphAsync();
        StateHasChanged();
    }

    private async Task ToggleEnabled(StepProjection projection, bool value)
    {
        try
        {
            ArgumentNullException.ThrowIfNull(Steps);
            var step = Steps.First(s => s.StepId == projection.StepId);
            await Mediator.SendAsync(new ToggleStepsCommand(step.StepId, value));
            step.IsEnabled = value;
            await LoadGraphAsync();
        }
        catch (Exception ex)
        {
            Toaster.AddError("Error toggling step", ex.Message);
        }
    }

    private async Task DeleteStep(StepProjection projection)
    {
        try
        {
            ArgumentNullException.ThrowIfNull(Steps);
            var step = Steps.First(s => s.StepId == projection.StepId);
            var result = await Confirmer.ConfirmAsync("", $"Are you sure you want to delete step \"{step.StepName}\"?");
            if (!result)
            {
                return;
            }

            await Mediator.SendAsync(new DeleteStepsCommand(step.StepId));
            Steps?.Remove(step);
            // Remove the deleted step from dependencies.
            foreach (var dependant in Steps?.Where(s => s.Dependencies.Any(d => d.DependantOnStepId == step.StepId)) ?? [])
            {
                var dependency = dependant.Dependencies.First(d => d.DependantOnStepId == step.StepId);
                dependant.Dependencies.Remove(dependency);
            }

            await LoadGraphAsync();
            StateHasChanged();
        }
        catch (Exception ex)
        {
            Toaster.AddError("Error deleting step", ex.Message);
        }
    }

    private Task<AutosuggestDataProviderResult<StepProjection>> ProvideSuggestions(AutosuggestDataProviderRequest request)
    {
        ArgumentNullException.ThrowIfNull(stepSlims);
        var filteredModules = stepSlims.Where(s => s.StepName?.ContainsIgnoreCase(request.UserInput) ?? false);
        return Task.FromResult(new AutosuggestDataProviderResult<StepProjection>
        {
            Data = filteredModules
        });
    }
}
