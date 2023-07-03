using Biflow.DataAccess;
using Biflow.DataAccess.Models;
using Biflow.Ui.Core;
using Biflow.Ui.Shared.Executions;
using Biflow.Ui.Shared.StepEditModal;
using Biflow.Ui.Shared.StepsBatchEdit;
using Havit.Blazor.Components.Web;
using Havit.Blazor.Components.Web.Bootstrap;
using Microsoft.AspNetCore.Components;
using Microsoft.EntityFrameworkCore;

namespace Biflow.Ui.Shared.JobDetails;

public partial class StepsComponent : ComponentBase
{
    [Inject] private IDbContextFactory<BiflowContext> DbFactory { get; set; } = null!;
        
    [Inject] private DbHelperService DbHelperService { get; set; } = null!;
    
    [Inject] private IHttpContextAccessor HttpContextAccessor { get; set; } = null!;
    
    [Inject] private IHxMessengerService Messenger { get; set; } = null!;

    [Inject] private NavigationManager NavigationManager { get; set; } = null!;
    
    [CascadingParameter] public Job? Job { get; set; }
    
    [CascadingParameter(Name = "SortSteps")] public Action? SortSteps { get; set; }

    [CascadingParameter] public List<Step>? Steps { get; set; }
    
    [CascadingParameter] public List<Job>? Jobs { get; set; }
    
    [Parameter] public List<SqlConnectionInfo>? SqlConnections { get; set; }
    
    [Parameter] public List<AnalysisServicesConnectionInfo>? AsConnections { get; set; }
    
    [Parameter] public List<PipelineClient>? PipelineClients { get; set; }
    
    [Parameter] public List<AppRegistration>? AppRegistrations { get; set; }
    
    [Parameter] public List<FunctionApp>? FunctionApps { get; set; }

    [Parameter] public Guid? InitialStepId { get; set; }

    private IEnumerable<Step> FilteredSteps => Steps?
        .Where(step => stateFilter switch { StateFilter.Enabled => step.IsEnabled, StateFilter.Disabled => !step.IsEnabled, _ => true })
        .Where(step => !StepNameFilter.Any() || (step.StepName?.ContainsIgnoreCase(StepNameFilter) ?? false))
        .Where(step => !StepDescriptionFilter.Any() || (step.StepDescription?.ContainsIgnoreCase(StepDescriptionFilter) ?? false))
        .Where(step => !SqlStatementFilter.Any() || step is SqlStep sql && (sql.SqlStatement?.ContainsIgnoreCase(SqlStatementFilter) ?? false))
        .Where(step => TagsFilterSet.All(tag => step.Tags.Any(t => t.TagName == tag.TagName)))
        .Where(step => !StepTypeFilter.Any() || StepTypeFilter.Contains(step.StepType))
        .Where(step => !ConnectionFilter.Any() || step is IHasConnection conn && ConnectionFilter.Any(f => f.ConnectionId == conn.ConnectionId))
        .Where(step => AdvancedFiltersOffcanvas?.EvaluatePredicates(step) ?? true)
        ?? Enumerable.Empty<Step>();

    private IEnumerable<Tag> Tags => Steps?
        .SelectMany(step => step.Tags)
        .DistinctBy(t => t.TagName)
        .OrderBy(t => t.TagName)
        ?? Enumerable.Empty<Tag>();

    private HashSet<Step> SelectedSteps { get; set; } = new();

    private Dictionary<StepType, IStepEditModal?> StepEditModals { get; } = new();

    private StepsBatchEditTagsModal? BatchEditTagsModal { get; set; }

    private StepDetailsModal? StepDetailsModal { get; set; }

    private StepHistoryOffcanvas? StepHistoryOffcanvas { get; set; }

    private ExecuteModal? ExecuteModal { get; set; }

    private AdvancedFiltersOffcanvas? AdvancedFiltersOffcanvas { get; set; }

    private string StepNameFilter { get; set; } = string.Empty;
    private string StepDescriptionFilter { get; set; } = string.Empty;
    private string SqlStatementFilter { get; set; } = string.Empty;
    private HashSet<Tag> TagsFilterSet { get; set; } = new();
    private HashSet<StepType> StepTypeFilter { get; } = new();
    private HashSet<ConnectionInfoBase> ConnectionFilter { get; set;} = new();

    private Guid? LastStartedExecutionId { get; set; }

    private bool ShowDetails { get; set; } = false;

    private bool InitialStepModalShouldOpen { get; set; } = true;

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (InitialStepModalShouldOpen && InitialStepId is Guid stepId)
        {
            var step = Steps?.FirstOrDefault(s => s.StepId == stepId);
            if (step is not null)
            {
                InitialStepModalShouldOpen = false;
                var editModal = StepEditModals.GetValueOrDefault(step.StepType);
                if (editModal is not null)
                {
                    await editModal.ShowAsync(stepId);
                    return;
                }
                await StepDetailsModal.LetAsync(x => x.ShowAsync(step));
            }
        }
    }

    private bool IsStepTypeDisabled(StepType type) => type switch
    {
        StepType.Sql or StepType.Package => SqlConnections?.Any() == false,
        StepType.Pipeline => PipelineClients?.Any() == false,
        StepType.Function => FunctionApps?.Any() == false,
        StepType.Dataset => AppRegistrations?.Any() == false,
        StepType.Job => Jobs is null || Jobs.Count == 1,
        StepType.AgentJob => SqlConnections?.Any() == false,
        StepType.Tabular => AsConnections?.Any() == false,
        _ => false,
    };

    private enum StateFilter { All, Enabled, Disabled }

    private StateFilter stateFilter = StateFilter.All;

    private async Task ShowEditModal(Step step) => await OpenStepEditModal(step.StepId, step.StepType);

    private async Task ShowNewStepModal(StepType stepType) => await OpenStepEditModal(Guid.Empty, stepType);

    private async Task OpenStepEditModal(Guid stepId, StepType? stepType)
    {
        if (stepType is not null)
            await StepEditModals[(StepType)stepType].LetAsync(x => x.ShowAsync(stepId));
    }

    private void ToggleAllStepsSelected(bool value)
    {
        if (value)
        {
            var stepsToAdd = FilteredSteps.Where(s => !SelectedSteps.Contains(s));
            foreach (var s in stepsToAdd) SelectedSteps.Add(s);
        }
        else
        {
            SelectedSteps.Clear();
        }
    }

    private async Task DeleteSelectedSteps()
    {
        try
        {
            using var context = DbFactory.CreateDbContext();
            foreach (var step in SelectedSteps)
            {
                context.Steps.Remove(step);
            }
            await context.SaveChangesAsync();
            foreach (var step in SelectedSteps)
            {
                Steps?.Remove(step);

                // Remove the deleted step from dependencies.
                foreach (var dependant in Steps?.Where(s => s.Dependencies.Any(d => d.DependantOnStepId == step.StepId)) ?? Enumerable.Empty<Step>())
                {
                    var dependency = dependant.Dependencies.First(d => d.DependantOnStepId == step.StepId);
                    dependant.Dependencies.Remove(dependency);
                }
            }
            SelectedSteps.Clear();
        }
        catch (Exception ex)
        {
            Messenger.AddError("Error deleting step", ex.Message);
        }
    }

    private async Task ToggleEnabled(ChangeEventArgs args, Step step)
    {
        bool value = (bool)args.Value!;
        try
        {
            using var context = DbFactory.CreateDbContext();
            context.Attach(step);
            step.IsEnabled = value;
            await context.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            Messenger.AddError("Error toggling step", ex.Message);
        }
    }

    private async Task DeleteStep(Step step)
    {
        try
        {
            using var context = DbFactory.CreateDbContext();
            context.Steps.Remove(step);
            await context.SaveChangesAsync();
            Steps?.Remove(step);
            SelectedSteps.Remove(step);

            // Remove the deleted step from dependencies.
            foreach (var dependant in Steps?.Where(s => s.Dependencies.Any(d => d.DependantOnStepId == step.StepId)) ?? Enumerable.Empty<Step>())
            {
                var dependency = dependant.Dependencies.First(d => d.DependantOnStepId == step.StepId);
                dependant.Dependencies.Remove(dependency);
            }
        }
        catch (Exception ex)
        {
            Messenger.AddError("Error deleting step", ex.Message);
        }
    }

    private async Task CopyStep(Step step, Job job)
    {
        try
        {
            string user = HttpContextAccessor.HttpContext?.User?.Identity?.Name ?? throw new ArgumentNullException(nameof(user), "User was null");
            var suffix = Job?.JobId == job.JobId ? " - Copy" : string.Empty;
            Guid createdStepId = await DbHelperService.StepCopyAsync(step.StepId, job.JobId, user, suffix);
            // If the steps was copied to this job, reload steps.
            if (Job?.JobId == job.JobId)
            {
                using var context = DbFactory.CreateDbContext();
                var createdStep = await context.Steps
                    .AsNoTrackingWithIdentityResolution()
                    .Include(step => step.Dependencies)
                    .Include(step => step.Sources)
                    .Include(step => step.Targets)
                    .Include(step => step.Tags)
                    .Include(step => step.ExecutionConditionParameters)
                    .Include(nameof(IHasStepParameters.StepParameters))
                    .FirstAsync(step_ => step_.StepId == createdStepId);
                Steps?.Add(createdStep);
                SortSteps?.Invoke();
            }
        }
        catch (Exception ex)
        {
            Messenger.AddError("Error copying step", ex.Message);
        }
    }

    private async Task CopySelectedSteps(Job job)
    {
        try
        {
            string user = HttpContextAccessor.HttpContext?.User?.Identity?.Name ?? throw new ArgumentNullException(nameof(user), "User was null");
            var suffix = Job?.JobId == job.JobId ? " - Copy" : string.Empty;
            foreach (var step in SelectedSteps)
            {
                Guid createdStepId = await DbHelperService.StepCopyAsync(step.StepId, job.JobId, user, suffix);
                // If the steps was copied to this job, reload steps.
                if (Job?.JobId == job.JobId)
                {
                    using var context = DbFactory.CreateDbContext();
                    var createdStep = await context.Steps
                        .AsNoTrackingWithIdentityResolution()
                        .Include(step => step.Dependencies)
                        .Include(step => step.Sources)
                        .Include(step => step.Targets)
                        .Include(step => step.Tags)
                        .Include(step => step.ExecutionConditionParameters)
                        .Include(nameof(IHasStepParameters.StepParameters))
                        .FirstAsync(step_ => step_.StepId == createdStepId);
                    Steps?.Add(createdStep);
                    SortSteps?.Invoke();
                }
            }
        }
        catch (Exception ex)
        {
            Messenger.AddError("Error copying steps", ex.Message);
        }
    }

    private void OnStepSubmit(Step step)
    {
        var existingStep = Steps?.FirstOrDefault(s => s.StepId == step.StepId);
        if (existingStep is not null)
        {
            Steps?.Remove(existingStep);
        }
        Steps?.Add(step);
        SortSteps?.Invoke();

        var selectedStep = SelectedSteps.FirstOrDefault(s => s.StepId == step.StepId);
        if (selectedStep is not null)
        {
            SelectedSteps.Remove(selectedStep);
            SelectedSteps.Add(step);
        }
    }

    private void OnExecutionStarted(Guid executionId)
    {
        LastStartedExecutionId = executionId;
    }

}
