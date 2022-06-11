using Biflow.DataAccess;
using Biflow.DataAccess.Models;
using Biflow.Ui.Core;
using Biflow.Ui.Shared.Executions;
using Biflow.Ui.Shared.StepEditModal;
using Havit.Blazor.Components.Web;
using Havit.Blazor.Components.Web.Bootstrap;
using Microsoft.AspNetCore.Components;
using Microsoft.EntityFrameworkCore;

namespace Biflow.Ui.Shared.JobDetails;

public partial class StepsComponent : ComponentBase
{
    [Inject] private IDbContextFactory<BiflowContext> DbFactory { get; set; } = null!;
    [Inject] private MarkupHelperService MarkupHelper { get; set; } = null!;
    [Inject] private DbHelperService DbHelperService { get; set; } = null!;
    [Inject] private IHttpContextAccessor HttpContextAccessor { get; set; } = null!;
    [Inject] private IHxMessengerService Messenger { get; set; } = null!;
    [Parameter] public Job? Job { get; set; }
    [Parameter] public IList<Job>? Jobs { get; set; }
    [Parameter] public List<Step>? Steps { get; set; }
    [Parameter] public List<SqlConnectionInfo>? SqlConnections { get; set; }
    [Parameter] public List<AnalysisServicesConnectionInfo>? AsConnections { get; set; }
    [Parameter] public List<DataFactory>? DataFactories { get; set; }
    [Parameter] public List<AppRegistration>? AppRegistrations { get; set; }
    [Parameter] public List<FunctionApp>? FunctionApps { get; set; }

    private IEnumerable<Step> FilteredSteps => Steps?
        .Where(step => !StepNameFilter.Any() || (step.StepName?.ContainsIgnoreCase(StepNameFilter) ?? false))
        .Where(step => !StepDescriptionFilter.Any() || (step.StepDescription?.ContainsIgnoreCase(StepDescriptionFilter) ?? false))
        .Where(step => !SqlStatementFilter.Any() || step is SqlStep sql && (sql.SqlStatement?.ContainsIgnoreCase(SqlStatementFilter) ?? false))
        .Where(step => TagsFilterSet.All(tag => step.Tags.Any(t => t.TagName == tag.TagName)))
        .Where(step => !StepTypeFilter.Any() || StepTypeFilter.Contains(step.StepType))
        .Where(step => !ConnectionFilter.Any()
                        || step is SqlStep sql && ConnectionFilter.Any(f => f.ConnectionId == sql.ConnectionId)
                        || step is PackageStep package && ConnectionFilter.Any(f => f.ConnectionId == package.ConnectionId)
                        || step is AgentJobStep agent && ConnectionFilter.Any(f => f.ConnectionId == agent.ConnectionId))
        ?? Enumerable.Empty<Step>();

    private IEnumerable<Tag> Tags => Steps?
        .SelectMany(step => step.Tags)
        .Select(tag => tag with { Steps = null! })
        .Distinct()
        .OrderBy(t => t.TagName)
        ?? Enumerable.Empty<Tag>();

    private HashSet<Step> SelectedSteps { get; set; } = new();

    private JobParametersModal JobParametersModal { get; set; } = null!;
    private JobConcurrencyModal JobConcurrencyModal { get; set; } = null!;
    private SynchronizeDependenciesModal SynchronizeDependenciesModal { get; set; } = null!;

    private Dictionary<StepType, IStepEditModal> StepEditModals { get; } = new();

    private StepDetailsModal StepDetailsModal { get; set; } = null!;
    private Step? DetailsModalStep { get; set; }

    private StepHistoryOffcanvas StepHistoryOffcanvas { get; set; } = null!;
    private Step? HistoryModalStep { get; set; }

    private ExecuteModal ExecuteModal { get; set; } = null!;

    private bool ShowExecutionAlert { get; set; } = false;

    private string StepNameFilter { get; set; } = string.Empty;
    private string StepDescriptionFilter { get; set; } = string.Empty;
    private string SqlStatementFilter { get; set; } = string.Empty;
    private HashSet<Tag> TagsFilterSet { get; set; } = new();
    private HashSet<StepType> StepTypeFilter { get; } = new();
    private HashSet<SqlConnectionInfo> ConnectionFilter { get; set;} = new();

    private JobExecutionDetailsModal JobExecutionModal { get; set; } = null!;
    private Guid SelectedJobExecutionId { get; set; }

    private bool ShowDetails { get; set; } = false;

    private bool IsStepTypeDisabled(StepType type) => type switch
    {
        StepType.Sql or StepType.Package => SqlConnections?.Any() == false,
        StepType.Pipeline => DataFactories?.Any() == false,
        StepType.Function => FunctionApps?.Any() == false,
        StepType.Dataset => AppRegistrations?.Any() == false,
        StepType.Job => Jobs is null || Jobs.Count == 1,
        StepType.AgentJob => SqlConnections?.Any() == false,
        StepType.Tabular => AsConnections?.Any() == false,
        _ => false,
    };

    private async Task ShowEditModal(Step step) => await OpenStepEditModal(step.StepId, step.StepType);

    private async Task ShowNewStepModal(StepType stepType) => await OpenStepEditModal(Guid.Empty, stepType);

    private async Task OpenStepEditModal(Guid stepId, StepType? stepType)
    {
        if (stepType is not null)
            await StepEditModals[(StepType)stepType].ShowAsync(stepId);
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
                    .Include(step => (step as ParameterizedStep)!.StepParameters)
                    .FirstAsync(step_ => step_.StepId == createdStepId);
                Steps?.Add(createdStep);
                SortSteps();
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
                        .Include(step => (step as ParameterizedStep)!.StepParameters)
                        .FirstAsync(step_ => step_.StepId == createdStepId);
                    Steps?.Add(createdStep);
                    SortSteps();
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
        SortSteps();
    }

    private void SortSteps()
    {
        if (Job is null || Steps is null) return;
        try
        {
            if (Job.UseDependencyMode)
            {
                var comparer = new TopologicalStepComparer(Steps);
                Steps.Sort(comparer);
            }
            else
            {
                Steps.Sort();
            }
        }
        catch (Exception ex)
        {
            Messenger.AddError("Error sorting steps", ex.Message);
        }
    }

    private void OnSynchronizeDependenciesModalClosed()
    {
        SortSteps();
        StateHasChanged();
    }

    private async Task ShowStepDetailsModal(Step step)
    {
        DetailsModalStep = step;
        await StepDetailsModal.Modal.ShowAsync();
    }

    private async Task ShowStepHistoryOffcanvas(Step step)
    {
        // Do not unnecessarily set the component parameter and start its data load.
        if (step != HistoryModalStep)
            HistoryModalStep = step;

        await StepHistoryOffcanvas.ShowAsync();
    }

    private void OnExecutionStarted(Guid executionId)
    {
        SelectedJobExecutionId = executionId;
        ShowExecutionAlert = true;
    }

    private async Task OpenJobExecutionModal() => await JobExecutionModal.ShowAsync();
}
