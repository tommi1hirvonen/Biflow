using Biflow.Ui.Shared.Executions;
using Biflow.Ui.Shared.StepEditModal;
using Biflow.Ui.Shared.StepsBatchEdit;

namespace Biflow.Ui.Shared.JobDetails;

public partial class StepsList : ComponentBase
{
    [Inject] private ToasterService Toaster { get; set; } = null!;
    [Inject] private IHxMessageBoxService Confirmer { get; set; } = null!;
    [Inject] private IMediator Mediator { get; set; } = null!;
    
    [CascadingParameter] public Job? Job { get; set; }
    
    [CascadingParameter(Name = "SortSteps")] public Action? SortSteps { get; set; }

    [CascadingParameter] public List<Step>? Steps { get; set; }
    
    [CascadingParameter] public List<Job>? Jobs { get; set; }
    
    [Parameter] public List<ConnectionBase>? SqlConnections { get; set; }

    [Parameter] public List<MsSqlConnection>? MsSqlConnections { get; set; }

    [Parameter] public List<AnalysisServicesConnection>? AsConnections { get; set; }
    
    [Parameter] public List<PipelineClient>? PipelineClients { get; set; }
    
    [Parameter] public List<AppRegistration>? AppRegistrations { get; set; }
    
    [Parameter] public List<FunctionApp>? FunctionApps { get; set; }

    [Parameter] public List<QlikCloudClient>? QlikCloudClients { get; set; }

    [Parameter] public List<DatabricksWorkspace>? DatabricksWorkspaces { get; set; }

    [Parameter] public List<Credential>? Credentials { get; set; }

    [Parameter] public Guid? InitialStepId { get; set; }

    private readonly HashSet<StepType> stepTypeFilter = [];
    private readonly Dictionary<StepType, IStepEditModal?> stepEditModals = [];
    private readonly HashSet<ConnectionBase> connectionFilter = [];
    private readonly HashSet<StepTag> tagsFilterSet = [];

    private HashSet<Step> selectedSteps = [];
    private StepsBatchEditTagsModal? batchEditTagsModal;
    private StepsBatchEditExecPhaseModal? batchEditExecPhaseModal;
    private StepsBatchEditRenameModal? batchEditRenameModal;
    private StepsBatchEditRetriesModal? batchEditRetriesModal;
    private StepsBatchEditRetryIntervalModal? batchEditRetryIntervalModal;
    private StepsCopyOffcanvas? stepsCopyOffcanvas;
    private StepDetailsModal? stepDetailsModal;
    private StepHistoryOffcanvas? stepHistoryOffcanvas;
    private ExecuteModal? executeModal;
    private AdvancedFiltersOffcanvas? advancedFiltersOffcanvas;
    private string stepNameFilter = string.Empty;
    private bool showDetails = false;
    private bool initialStepModalShouldOpen = true;
    private StateFilter stateFilter = StateFilter.All;

    private enum StateFilter { All, Enabled, Disabled }

    private IEnumerable<Step> FilteredSteps => Steps?
        .Where(step => stateFilter switch { StateFilter.Enabled => step.IsEnabled, StateFilter.Disabled => !step.IsEnabled, _ => true })
        .Where(step => stepNameFilter.Length == 0 || (step.StepName?.ContainsIgnoreCase(stepNameFilter) ?? false))
        .Where(step => tagsFilterSet.All(tag => step.Tags.Any(t => t.TagName == tag.TagName)))
        .Where(step => stepTypeFilter.Count == 0 || stepTypeFilter.Contains(step.StepType))
        .Where(step => connectionFilter.Count == 0 || step is IHasConnection conn && connectionFilter.Any(f => f.ConnectionId == conn.ConnectionId))
        .Where(step => advancedFiltersOffcanvas?.EvaluatePredicates(step) ?? true)
        ?? [];

    private IEnumerable<StepTag> Tags => Steps?
        .SelectMany(step => step.Tags)
        .DistinctBy(t => t.TagName)
        .OrderBy(t => t.TagName)
        .AsEnumerable()
        ?? [];

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (initialStepModalShouldOpen && InitialStepId is Guid stepId)
        {
            var step = Steps?.FirstOrDefault(s => s.StepId == stepId);
            if (step is not null)
            {
                initialStepModalShouldOpen = false;
                var editModal = stepEditModals.GetValueOrDefault(step.StepType);
                if (editModal is not null)
                {
                    await editModal.ShowAsync(stepId);
                    return;
                }
                await stepDetailsModal.LetAsync(x => x.ShowAsync(step));
            }
        }
    }

    private (bool, string) IsStepTypeDisabled(StepType type) => type switch
    {
        StepType.Sql => ((SqlConnections?.Count ?? 0) == 0, "No SQL connections defined"),
        StepType.Package => ((MsSqlConnections?.Count ?? 0) == 0, "No MS SQL connections defined"),
        StepType.Pipeline => ((PipelineClients?.Count ?? 0) == 0, "No pipeline clients defined"),
        StepType.Function => ((FunctionApps?.Count ?? 0) == 0, "No Function Apps defined"),
        StepType.Dataset => ((AppRegistrations?.Count ?? 0) == 0, "No app registrations defined"),
        StepType.Job => (Jobs is null || Jobs.Count == 1, ""),
        StepType.AgentJob => ((MsSqlConnections?.Count ?? 0) == 0, "No MS SQL connections defined"),
        StepType.Tabular => ((AsConnections?.Count ?? 0) == 0, "No Analysis Services connections defined"),
        StepType.Qlik => ((QlikCloudClients?.Count ?? 0) == 0, "No Qlik Cloud clients defined"),
        StepType.Databricks => ((DatabricksWorkspaces?.Count ?? 0) == 0, "No Databricks workspaces defined"),
        _ => (false, "")
    };

    private async Task ShowEditModal(Step step) => await OpenStepEditModal(step.StepId, step.StepType);

    private async Task ShowNewStepModal(StepType stepType) => await OpenStepEditModal(Guid.Empty, stepType);

    private async Task OpenStepEditModal(Guid stepId, StepType? stepType)
    {
        if (stepType is not null)
            await stepEditModals[(StepType)stepType].LetAsync(x => x.ShowAsync(stepId));
    }

    private void ToggleAllStepsSelected(bool value)
    {
        if (value)
        {
            var stepsToAdd = FilteredSteps.Where(s => !selectedSteps.Contains(s));
            foreach (var s in stepsToAdd) selectedSteps.Add(s);
        }
        else
        {
            selectedSteps.Clear();
        }
    }

    private async Task DeleteSelectedSteps()
    {
        if (!await Confirmer.ConfirmAsync("Delete steps", $"Are you sure you want to delete {selectedSteps.Count} steps?"))
        {
            return;
        }
        try
        {
            await Mediator.SendAsync(new DeleteStepsCommand(selectedSteps));
            foreach (var step in selectedSteps)
            {
                Steps?.Remove(step);

                // Remove the deleted step from dependencies.
                foreach (var dependant in Steps?.Where(s => s.Dependencies.Any(d => d.DependantOnStepId == step.StepId)) ?? [])
                {
                    var dependency = dependant.Dependencies.First(d => d.DependantOnStepId == step.StepId);
                    dependant.Dependencies.Remove(dependency);
                }
            }
            selectedSteps.Clear();
        }
        catch (Exception ex)
        {
            Toaster.AddError("Error deleting step", ex.Message);
        }
    }

    private async Task ToggleEnabled(ChangeEventArgs args, Step step)
    {
        bool value = (bool)args.Value!;
        try
        {
            await Mediator.SendAsync(new ToggleStepsCommand(step.StepId, value));
            step.IsEnabled = value;
            var message = value ? "Step enabled" : "Step disabled";
            Toaster.AddSuccess(message, 2500);
        }
        catch (Exception ex)
        {
            Toaster.AddError("Error toggling step", ex.Message);
        }
    }

    private async Task DeleteStep(Step step)
    {
        if (!await Confirmer.ConfirmAsync("Delete step", $"Are you sure you want to delete \"{step.StepName}\"?"))
        {
            return;
        }
        try
        {
            await Mediator.SendAsync(new DeleteStepsCommand(step.StepId));
            Steps?.Remove(step);
            selectedSteps.Remove(step);

            // Remove the deleted step from dependencies.
            foreach (var dependant in Steps?.Where(s => s.Dependencies.Any(d => d.DependantOnStepId == step.StepId)) ?? [])
            {
                var dependency = dependant.Dependencies.First(d => d.DependantOnStepId == step.StepId);
                dependant.Dependencies.Remove(dependency);
            }
        }
        catch (Exception ex)
        {
            Toaster.AddError("Error deleting step", ex.Message);
        }
    }

    private void OnStepsCopied(IEnumerable<Step> copies)
    {
        ArgumentNullException.ThrowIfNull(Job);
        var steps = copies.Where(s => s.JobId == Job.JobId).ToArray();
        if (steps.Length > 0)
        {
            Steps?.AddRange(steps);
            SortSteps?.Invoke();
        }
    }

    private void OnStepSubmit(Step step)
    {
        var index = Steps?.FindIndex(s => s.StepId == step.StepId);
        if (index is int i and >= 0)
        {
            Steps?.RemoveAt(i);
            Steps?.Insert(i, step);
        }
        else
        {
            Steps?.Add(step);
        }

        SortSteps?.Invoke();

        var selectedStep = selectedSteps.FirstOrDefault(s => s.StepId == step.StepId);
        if (selectedStep is not null)
        {
            selectedSteps.Remove(selectedStep);
            selectedSteps.Add(step);
        }
    }

    private void OnStepsSubmit(IEnumerable<Step> steps)
    {
        foreach (var step in steps.ToArray())
        {
            var index = Steps?.FindIndex(s => s.StepId == step.StepId);
            if (index is int i and >= 0)
            {
                Steps?.RemoveAt(i);
                Steps?.Insert(i, step);
            }
            else
            {
                Steps?.Add(step);
            }
        }

        SortSteps?.Invoke();

        selectedSteps = steps.ToHashSet();
    }

    private async Task ToggleSelectedStepsEnabledAsync(bool enabled)
    {
        var steps = selectedSteps
            .Where(s => s.IsEnabled != enabled)
            .ToArray();
        try
        {
            await Mediator.SendAsync(new ToggleStepsCommand(steps, enabled));
            foreach (var step in steps)
            {
                step.IsEnabled = enabled;
            }
        }
        catch (Exception ex)
        {
            Toaster.AddError("Error toggling steps", ex.Message);
        }
    }
}
