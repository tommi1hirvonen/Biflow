using Biflow.DataAccess;
using Biflow.DataAccess.Models;
using Biflow.Ui.Core;
using Havit.Blazor.Components.Web;
using Havit.Blazor.Components.Web.Bootstrap;
using Microsoft.AspNetCore.Components;
using Microsoft.EntityFrameworkCore;

namespace Biflow.Ui.Pages;

[Route("/jobs/{Id:guid}/{DetailsPage}")]
public partial class JobDetails : ComponentBase
{
    [Inject] private IDbContextFactory<BiflowContext> DbFactory { get; set; } = null!;
    [Inject] private ISchedulerService SchedulerService { get; set; } = null!;
    [Inject] private NavigationManager NavigationManager { get; set; } = null!;
    [Inject] private IHxMessengerService Messenger { get; set; } = null!;

    [Parameter] public string? DetailsPage { get; set; }

    [Parameter] public Guid Id { get; set; }

    private Job? Job { get; set; }

    private List<Job> Jobs { get; set; } = new();

    private List<Step> Steps { get; set; } = new();

    private List<SqlConnectionInfo>? SqlConnections { get; set; }
    private List<AnalysisServicesConnectionInfo>? AsConnections { get; set; }
    private List<PipelineClient>? PipelineClients { get; set; }
    private List<AppRegistration>? AppRegistrations { get; set; }
    private List<FunctionApp>? FunctionApps { get; set; }

    private bool DescriptionOpen { get; set; }

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
        Job = await context.Jobs
            .AsNoTrackingWithIdentityResolution()
            .Include(job => job.Category)
            .Include(job => job.JobParameters)
            .FirstAsync(job => job.JobId == Id);
        Job.JobParameters = Job.JobParameters.OrderBy(p => p.ParameterName).ToList();
        Steps = await context.Steps
            .AsNoTrackingWithIdentityResolution()
            .Include(step => step.Dependencies)
            .Include(step => step.Sources)
            .Include(step => step.Targets)
            .Include(step => step.Tags)
            .Include(step => step.StepParameters)
            .Include(step => step.ExecutionConditionParameters)
            .Where(step => step.JobId == Job.JobId)
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
        job.JobParameters = Job.JobParameters;
        Job = job;
        StateHasChanged();
    }

    private void OnJobParametersSet(IList<JobParameter> parameters)
    {
        if (Job is null) return;

        Job.JobParameters = parameters;
    }

    private async Task DeleteJob()
    {
        try
        {
            ArgumentNullException.ThrowIfNull(Job);
            using var context = DbFactory.CreateDbContext();
            context.Jobs.Remove(Job);
            await context.SaveChangesAsync();
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