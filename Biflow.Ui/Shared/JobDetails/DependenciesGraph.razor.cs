using Biflow.Ui.Shared.Executions;
using Biflow.Ui.Shared.StepEditModal;

namespace Biflow.Ui.Shared.JobDetails;

public partial class DependenciesGraph(
    ToasterService toaster,
    IDbContextFactory<AppDbContext> dbContextFactory,
    IHxMessageBoxService confirmer,
    IMediator mediator) : ComponentBase
{
    [CascadingParameter] public Job? Job { get; set; }

    [CascadingParameter] public List<Step>? Steps { get; set; }

    [CascadingParameter(Name = "SortSteps")] public Action? SortSteps { get; set; }

    [Parameter] public List<SqlConnectionBase>? SqlConnections { get; set; }

    [Parameter] public List<MsSqlConnection>? MsSqlConnections { get; set; }

    [Parameter] public List<AnalysisServicesConnection>? AsConnections { get; set; }

    [Parameter] public List<PipelineClient>? PipelineClients { get; set; }

    [Parameter] public List<AzureCredential>? AzureCredentials { get; set; }

    [Parameter] public List<FunctionApp>? FunctionApps { get; set; }

    [Parameter] public List<QlikCloudEnvironment>? QlikCloudClients { get; set; }

    [Parameter] public List<DatabricksWorkspace>? DatabricksWorkspaces { get; set; }

    [Parameter] public List<DbtAccount>? DbtAccounts { get; set; }
    
    [Parameter] public List<ScdTable>? ScdTables { get; set; }

    [Parameter] public List<Credential>? Credentials { get; set; }

    [Parameter] public Guid? InitialStepId { get; set; }

    private readonly ToasterService _toaster = toaster;
    private readonly IDbContextFactory<AppDbContext> _dbContextFactory = dbContextFactory;
    private readonly IHxMessageBoxService _confirmer = confirmer;
    private readonly IMediator _mediator = mediator;
    private readonly Dictionary<StepType, IStepEditModal?> _stepEditModals = [];
    private readonly HashSet<StepTag> _tagsFilterSet = [];

    private FilterDropdownMode _tagsFilterMode = FilterDropdownMode.Any;
    private DependencyGraph<StepProjection>? _dependencyGraph;
    private StepHistoryOffcanvas? _stepHistoryOffcanvas;
    private Guid? _stepFilter;
    private DependencyGraphDirection _direction = DependencyGraphDirection.LeftToRight;
    private List<StepProjection>? _stepSlims;

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

    private IEnumerable<StepTag> Tags => Steps?
         .SelectMany(step => step.Tags)
         .DistinctBy(t => t.TagName)
         .Order()
         .AsEnumerable() ?? [];

    private Func<StepProjection, bool> TagFilterPredicate => step =>
        (_tagsFilterMode is FilterDropdownMode.Any && (_tagsFilterSet.Count == 0 || _tagsFilterSet.Any(tag => step.Tags.Any(t => t.TagName == tag.TagName))))
        || (_tagsFilterMode is FilterDropdownMode.All && _tagsFilterSet.All(tag => step.Tags.Any(t => t.TagName == tag.TagName)));

    protected override void OnAfterRender(bool firstRender)
    {
        if (!firstRender) return;
        if (InitialStepId is not { } filterStepId) return;
        _stepFilter = filterStepId;
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

    private async Task LoadGraphAsync()
    {
        ArgumentNullException.ThrowIfNull(Job);
        ArgumentNullException.ThrowIfNull(_dependencyGraph);

        await using var context = await _dbContextFactory.CreateDbContextAsync();
        _stepSlims = await context.Steps
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
        if (_stepFilter is null)
        {
            var steps = _stepSlims.Where(TagFilterPredicate).ToArray();
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
            var startStep = _stepSlims.FirstOrDefault(s => s.StepId == _stepFilter);
            if (startStep is not null)
            {
                var steps = RecurseDependenciesBackward(startStep, _stepSlims, [], 0);
                steps.Remove(startStep);
                steps = RecurseDependenciesForward(startStep, _stepSlims, steps, 0);

                nodes = steps.Select(step => new DependencyGraphNode(
                    Id: step.StepId.ToString(),
                    Name: step.StepName ?? "",
                    CssClass: $"{(step.IsEnabled ? "enabled" : "disabled")} {(step.JobId != Job.JobId ? "external" : "internal")} {(step.StepId == _stepFilter ? "selected" : null)}",
                    TooltipText: $"{step.StepType}",
                    EnableOnClick: true
                )).ToList();
                edges = _stepSlims
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

        await _dependencyGraph.DrawAsync(nodes, edges, _direction);
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
            return processedSteps;
        }

        processedSteps.Add(step);

        // Get dependency steps.
        var dependencySteps = allSteps
            .Where(s => step.Dependencies.Any(d => d.DependentOnStepId == s.StepId))
            .ToList();

        // Loop through the dependencies and handle them recursively.
        foreach (var dependencyStep in dependencySteps)
        {
            RecurseDependenciesBackward(dependencyStep, allSteps, processedSteps, depth);
        }

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
            return processedSteps;
        }

        processedSteps.Add(step);

        var dependencySteps = allSteps
            .Where(s => s.Dependencies.Any(d => d.DependentOnStepId == step.StepId))
            .ToList();

        foreach (var dependencyStep in dependencySteps)
        {
            RecurseDependenciesForward(dependencyStep, allSteps, processedSteps, depth);
        }

        return processedSteps;
    }

    private Task OpenStepEditModalAsync(StepProjection step) =>
        _stepEditModals[step.StepType].LetAsync(x => x.ShowAsync(step.StepId, StepEditModalView.Dependencies));

    private async Task OnStepSubmit(Step step)
    {
        ArgumentNullException.ThrowIfNull(Steps);

        var index = Steps.FindIndex(s => s.StepId == step.StepId);
        if (index >= 0)
        {
            Steps.RemoveAt(index);
            Steps.Insert(index, step);
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
            var command = new ToggleStepEnabledCommand(step.StepId, value);
            await _mediator.SendAsync(command);
            step.IsEnabled = value;
            await LoadGraphAsync();
        }
        catch (Exception ex)
        {
            _toaster.AddError("Error toggling step", ex.Message);
        }
    }

    private async Task DeleteStep(StepProjection projection)
    {
        try
        {
            ArgumentNullException.ThrowIfNull(Steps);
            var step = Steps.First(s => s.StepId == projection.StepId);
            var result = await _confirmer.ConfirmAsync("", $"Are you sure you want to delete step \"{step.StepName}\"?");
            if (!result)
            {
                return;
            }

            await _mediator.SendAsync(new DeleteStepsCommand(step.StepId));
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
            _toaster.AddError("Error deleting step", ex.Message);
        }
    }

    private Task<AutosuggestDataProviderResult<StepProjection>> ProvideSuggestions(AutosuggestDataProviderRequest request)
    {
        ArgumentNullException.ThrowIfNull(_stepSlims);
        var filteredModules = _stepSlims.Where(s => s.StepName?.ContainsIgnoreCase(request.UserInput) ?? false);
        return Task.FromResult(new AutosuggestDataProviderResult<StepProjection>
        {
            Data = filteredModules
        });
    }
}
