using Biflow.DataAccess;
using Biflow.DataAccess.Models;
using Biflow.Ui.Core;
using Havit.Blazor.Components.Web;
using Havit.Blazor.Components.Web.Bootstrap;
using Microsoft.AspNetCore.Components;
using Microsoft.EntityFrameworkCore;

namespace Biflow.Ui.Pages;

[Route("/jobs/{Id:guid}/{DetailsPage}/{InitialStepId:guid?}")]
public partial class JobDetails : ComponentBase
{
    [Inject] private IDbContextFactory<AppDbContext> DbFactory { get; set; } = null!;
    [Inject] private ISchedulerService SchedulerService { get; set; } = null!;
    [Inject] private NavigationManager NavigationManager { get; set; } = null!;
    [Inject] private IHxMessengerService Messenger { get; set; } = null!;
    [Inject] private IHxMessageBoxService Confirmer { get; set; } = null!;

    [Parameter] public string DetailsPage { get; set; } = "steps";

    [Parameter] public Guid Id { get; set; }

    [Parameter] public Guid? InitialStepId { get; set; }

    private Job? Job { get; set; }

    private List<Job> Jobs { get; set; } = new();

    private List<Step> Steps { get; set; } = new();

    private List<SqlConnectionInfo>? SqlConnections { get; set; }
    private List<AnalysisServicesConnectionInfo>? AsConnections { get; set; }
    private List<PipelineClient>? PipelineClients { get; set; }
    private List<AppRegistration>? AppRegistrations { get; set; }
    private List<FunctionApp>? FunctionApps { get; set; }
    private List<QlikCloudClient>? QlikCloudClients { get; set; }

    private bool DescriptionOpen { get; set; }

    public static IQueryable<Step> BuildStepsQueryWithIncludes(AppDbContext context)
    {
        return context.Steps
            .Include(step => step.Dependencies)
            .Include(step => step.Sources)
            .Include(step => step.Targets)
            .Include(step => step.Tags)
            .Include(step => step.ExecutionConditionParameters)
            .ThenInclude(p => p.JobParameter)
            .Include($"{nameof(IHasStepParameters.StepParameters)}.{nameof(StepParameterBase.InheritFromJobParameter)}")
            .Include($"{nameof(IHasStepParameters.StepParameters)}.{nameof(StepParameterBase.ExpressionParameters)}")
            .Include(step => step.ExecutionConditionParameters);
    }

    protected override async Task OnInitializedAsync()
    {
        using var context = await DbFactory.CreateDbContextAsync();
        SqlConnections = await context.SqlConnections
            .AsNoTracking()
            .OrderBy(c => c.ConnectionName)
            .ToListAsync();
        AsConnections = await context.AnalysisServicesConnections
            .AsNoTracking()
            .OrderBy(c => c.ConnectionName)
            .ToListAsync();
        PipelineClients = await context.PipelineClients
            .AsNoTracking()
            .OrderBy(df => df.PipelineClientName)
            .ToListAsync();
        AppRegistrations = await context.AppRegistrations
            .AsNoTracking()
            .OrderBy(app => app.AppRegistrationName)
            .ToListAsync();
        FunctionApps = await context.FunctionApps
            .AsNoTracking()
            .OrderBy(app => app.FunctionAppName)
            .ToListAsync();
        QlikCloudClients = await context.QlikCloudClients
            .AsNoTracking()
            .OrderBy(c => c.QlikCloudClientName)
            .ToListAsync();
        Job = await context.Jobs
            .AsNoTrackingWithIdentityResolution()
            .Include(job => job.Category)
            .FirstAsync(job => job.JobId == Id);
        Steps = await BuildStepsQueryWithIncludes(context)
            .Where(step => step.JobId == Job.JobId)
            .AsNoTrackingWithIdentityResolution()
            .ToListAsync();
        Jobs = await context.Jobs
            .AsNoTrackingWithIdentityResolution()
            .Include(j => j.Category)
            .OrderBy(j => j.JobName)
            .ToListAsync();
        SortSteps();
    }

    private void SortSteps()
    {
        if (Job is null)
        {
            return;
        }
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
            StateHasChanged();
        }
        catch (Exception ex)
        {
            Messenger.AddError("Error sorting steps", ex.Message);
        }
    }

    private void OnJobUpdated(Job job)
    {
        if (Job is null)
        {
            return;
        }
        Job = job;
        StateHasChanged();
    }

    private async Task DeleteJob()
    {
        try
        {
            ArgumentNullException.ThrowIfNull(Job);
            using (var context1 = await DbFactory.CreateDbContextAsync())
            {
                var executingSteps = await context1.JobSteps
                    .Where(s => s.JobToExecuteId == Job.JobId)
                    .Include(s => s.Job)
                    .OrderBy(s => s.Job.JobName)
                    .ThenBy(s => s.StepName)
                    .ToListAsync();
                if (executingSteps.Any())
                {
                    var steps = string.Join("\n", executingSteps.Select(s => $"– {s.Job.JobName} – {s.StepName}"));
                    var message = $"""
                    This job has one or more referencing steps that execute this job:
                    {steps}
                    Removing the job will also remove these steps. Delete anyway?
                    """;
                    var confirmResult = await Confirmer.ConfirmAsync("", message);
                    if (!confirmResult)
                    {
                        return;
                    }
                }
            }
            using var context2 = DbFactory.CreateDbContext();
            context2.Jobs.Remove(Job);
            await context2.SaveChangesAsync();
            await SchedulerService.DeleteJobAsync(Job);
            NavigationManager.NavigateTo("jobs");
        }
        catch (Exception ex)
        {
            Messenger.AddError("Error deleting job", ex.Message);
        }
    }

    private async Task ToggleJobEnabled(ChangeEventArgs args)
    {
        try
        {
            var enabled = (bool)args.Value!;
            ArgumentNullException.ThrowIfNull(Job);
            using var context = DbFactory.CreateDbContext();
            Job.IsEnabled = enabled;
            await context.Jobs
                .Where(j => j.JobId == Job.JobId)
                .ExecuteUpdateAsync(j => j.SetProperty(p => p.IsEnabled, Job.IsEnabled));
            var message = Job.IsEnabled ? "Job enabled successfully" : "Job disabled successfully";
            Messenger.AddInformation(message);
        }
        catch (Exception ex)
        {
            Messenger.AddError("Error toggling job", ex.Message);
        }
    }
}