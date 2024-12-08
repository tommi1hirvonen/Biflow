﻿using Biflow.Ui.Shared.Executions;
using Biflow.Ui.Shared.StepEditModal;
using Biflow.Ui.Shared.StepsBatchEdit;

namespace Biflow.Ui.Shared.JobDetails;

public partial class StepsList(
    ToasterService toaster,
    IHxMessageBoxService confirmer,
    IMediator mediator) : ComponentBase
{
    [CascadingParameter] public Job? Job { get; set; }
    
    [CascadingParameter(Name = "SortSteps")] public Action? SortSteps { get; set; }

    [CascadingParameter] public List<Step>? Steps { get; set; }
    
    [CascadingParameter] public List<Job>? Jobs { get; set; }
    
    [Parameter] public List<SqlConnectionBase>? SqlConnections { get; set; }

    [Parameter] public List<MsSqlConnection>? MsSqlConnections { get; set; }

    [Parameter] public List<AnalysisServicesConnection>? AsConnections { get; set; }
    
    [Parameter] public List<PipelineClient>? PipelineClients { get; set; }
    
    [Parameter] public List<AzureCredential>? AppRegistrations { get; set; }
    
    [Parameter] public List<FunctionApp>? FunctionApps { get; set; }

    [Parameter] public List<QlikCloudEnvironment>? QlikCloudClients { get; set; }

    [Parameter] public List<DatabricksWorkspace>? DatabricksWorkspaces { get; set; }

    [Parameter] public List<DbtAccount>? DbtAccounts { get; set; }
    
    [Parameter] public List<ScdTable>? ScdTables { get; set; }

    [Parameter] public List<Credential>? Credentials { get; set; }

    [Parameter] public Guid? InitialStepId { get; set; }

    private readonly ToasterService _toaster = toaster;
    private readonly IHxMessageBoxService _confirmer = confirmer;
    private readonly IMediator _mediator = mediator;
    private readonly HashSet<StepType> _stepTypeFilter = [];
    private readonly Dictionary<StepType, IStepEditModal?> _stepEditModals = [];
    private readonly HashSet<StepTag> _tagsFilterSet = [];

    private HashSet<Step> _selectedSteps = [];
    private StepsBatchEditTagsModal? _batchEditTagsModal;
    private StepsBatchEditExecPhaseModal? _batchEditExecPhaseModal;
    private StepsBatchEditRenameModal? _batchEditRenameModal;
    private StepsBatchEditRetriesModal? _batchEditRetriesModal;
    private StepsBatchEditRetryIntervalModal? _batchEditRetryIntervalModal;
    private StepsCopyOffcanvas? _stepsCopyOffcanvas;
    private StepDetailsModal? _stepDetailsModal;
    private StepHistoryOffcanvas? _stepHistoryOffcanvas;
    private ExecuteModal? _executeModal;
    private AdvancedFiltersOffcanvas? _advancedFiltersOffcanvas;
    private string _stepNameFilter = string.Empty;
    private bool _showDetails = false;
    private bool _initialStepModalShouldOpen = true;
    private StateFilter _stateFilter = StateFilter.All;
    private FilterDropdownMode _tagsFilterMode = FilterDropdownMode.Any;

    private enum StateFilter { All, Enabled, Disabled }

    private IEnumerable<Step> FilteredSteps => Steps?
        .Where(step => _stateFilter switch { StateFilter.Enabled => step.IsEnabled, StateFilter.Disabled => !step.IsEnabled, _ => true })
        .Where(step => _stepNameFilter.Length == 0 || (step.StepName?.ContainsIgnoreCase(_stepNameFilter) ?? false))
        .Where(step =>
            (_tagsFilterMode is FilterDropdownMode.Any && (_tagsFilterSet.Count == 0 || _tagsFilterSet.Any(tag => step.Tags.Any(t => t.TagName == tag.TagName))))
            || (_tagsFilterMode is FilterDropdownMode.All && _tagsFilterSet.All(tag => step.Tags.Any(t => t.TagName == tag.TagName))))
        .Where(step => _stepTypeFilter.Count == 0 || _stepTypeFilter.Contains(step.StepType))
        .Where(step => _advancedFiltersOffcanvas?.EvaluatePredicates(step) ?? true)
        ?? [];

    private IEnumerable<StepTag> Tags => Steps?
        .SelectMany(step => step.Tags)
        .DistinctBy(t => t.TagName)
        .Order()
        .AsEnumerable()
        ?? [];

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (_initialStepModalShouldOpen && InitialStepId is { } stepId)
        {
            var step = Steps?.FirstOrDefault(s => s.StepId == stepId);
            if (step is not null)
            {
                _initialStepModalShouldOpen = false;
                var editModal = _stepEditModals.GetValueOrDefault(step.StepType);
                if (editModal is not null)
                {
                    await editModal.ShowAsync(stepId);
                    return;
                }
                await _stepDetailsModal.LetAsync(x => x.ShowAsync(step));
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
        StepType.Dbt => ((DbtAccounts?.Count ?? 0) == 0, "No dbt accounts defined"),
        StepType.Scd => ((ScdTables?.Count ?? 0) == 0, "No SCD tables defined"),
        _ => (false, "")
    };

    private async Task ShowEditModal(Step step) => await OpenStepEditModal(step.StepId, step.StepType);

    private async Task ShowNewStepModal(StepType stepType) => await OpenStepEditModal(Guid.Empty, stepType);

    private async Task OpenStepEditModal(Guid stepId, StepType? stepType)
    {
        if (stepType is not null)
            await _stepEditModals[(StepType)stepType].LetAsync(x => x.ShowAsync(stepId));
    }

    private void ToggleAllStepsSelected(bool value)
    {
        if (value)
        {
            var stepsToAdd = FilteredSteps.Where(s => !_selectedSteps.Contains(s));
            foreach (var s in stepsToAdd) _selectedSteps.Add(s);
        }
        else
        {
            _selectedSteps.Clear();
        }
    }

    private async Task DeleteSelectedSteps()
    {
        if (!await _confirmer.ConfirmAsync("Delete steps", $"Are you sure you want to delete {_selectedSteps.Count} steps?"))
        {
            return;
        }
        try
        {
            await _mediator.SendAsync(new DeleteStepsCommand(_selectedSteps));
            foreach (var step in _selectedSteps)
            {
                Steps?.Remove(step);

                // Remove the deleted step from dependencies.
                foreach (var dependant in Steps?.Where(s => s.Dependencies.Any(d => d.DependantOnStepId == step.StepId)) ?? [])
                {
                    var dependency = dependant.Dependencies.First(d => d.DependantOnStepId == step.StepId);
                    dependant.Dependencies.Remove(dependency);
                }
            }
            _selectedSteps.Clear();
        }
        catch (Exception ex)
        {
            _toaster.AddError("Error deleting step", ex.Message);
        }
    }

    private async Task ToggleEnabled(ChangeEventArgs args, Step step)
    {
        var value = (bool)args.Value!;
        try
        {
            await _mediator.SendAsync(new ToggleStepsCommand(step.StepId, value));
            step.IsEnabled = value;
            var message = value ? "Step enabled" : "Step disabled";
            _toaster.AddSuccess(message, 2500);
        }
        catch (Exception ex)
        {
            _toaster.AddError("Error toggling step", ex.Message);
        }
    }

    private async Task DeleteStep(Step step)
    {
        if (!await _confirmer.ConfirmAsync("Delete step", $"Are you sure you want to delete \"{step.StepName}\"?"))
        {
            return;
        }
        try
        {
            await _mediator.SendAsync(new DeleteStepsCommand(step.StepId));
            Steps?.Remove(step);
            _selectedSteps.Remove(step);

            // Remove the deleted step from dependencies.
            foreach (var dependant in Steps?.Where(s => s.Dependencies.Any(d => d.DependantOnStepId == step.StepId)) ?? [])
            {
                var dependency = dependant.Dependencies.First(d => d.DependantOnStepId == step.StepId);
                dependant.Dependencies.Remove(dependency);
            }
        }
        catch (Exception ex)
        {
            _toaster.AddError("Error deleting step", ex.Message);
        }
    }

    private void OnStepsCopied(IEnumerable<Step> copies)
    {
        ArgumentNullException.ThrowIfNull(Job);
        var steps = copies.Where(s => s.JobId == Job.JobId).ToArray();
        if (steps.Length <= 0) return;
        Steps?.AddRange(steps);
        SortSteps?.Invoke();
    }

    private void OnStepSubmit(Step step)
    {
        var index = Steps?.FindIndex(s => s.StepId == step.StepId);
        if (index is { } i and >= 0)
        {
            Steps?.RemoveAt(i);
            Steps?.Insert(i, step);
        }
        else
        {
            Steps?.Add(step);
        }

        SortSteps?.Invoke();

        var selectedStep = _selectedSteps.FirstOrDefault(s => s.StepId == step.StepId);
        if (selectedStep is not null)
        {
            _selectedSteps.Remove(selectedStep);
            _selectedSteps.Add(step);
        }
    }

    private void OnStepsSubmit(IEnumerable<Step> steps)
    {
        foreach (var step in steps.ToArray())
        {
            var index = Steps?.FindIndex(s => s.StepId == step.StepId);
            if (index is { } i and >= 0)
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

        _selectedSteps = steps.ToHashSet();
    }

    private async Task ToggleSelectedStepsEnabledAsync(bool enabled)
    {
        var steps = _selectedSteps
            .Where(s => s.IsEnabled != enabled)
            .ToArray();
        try
        {
            await _mediator.SendAsync(new ToggleStepsCommand(steps, enabled));
            foreach (var step in steps)
            {
                step.IsEnabled = enabled;
            }
        }
        catch (Exception ex)
        {
            _toaster.AddError("Error toggling steps", ex.Message);
        }
    }
}
