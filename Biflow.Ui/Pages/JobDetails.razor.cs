using Biflow.DataAccess;
using Biflow.DataAccess.Models;
using Biflow.Ui.Core;
using Havit.Blazor.Components.Web;
using Havit.Blazor.Components.Web.Bootstrap;
using Microsoft.AspNetCore.Components;
using Microsoft.EntityFrameworkCore;

namespace Biflow.Ui.Pages;

public partial class JobDetails : ComponentBase
{
    [Inject] private IDbContextFactory<BiflowContext> DbFactory { get; set; } = null!;
    [Inject] private MarkupHelperService MarkupHelper { get; set; } = null!;
    [Inject] private ISchedulerService SchedulerService { get; set; } = null!;
    [Inject] private NavigationManager NavigationManager { get; set; } = null!;
    [Inject] private IHxMessengerService Messenger { get; set; } = null!;

    [Parameter] public string? DetailsPage { get; set; }

    [Parameter] public Guid Id { get; set; }

    private Job Job { get; set; } = new();

    private List<Job> Jobs { get; set; } = new();

    private List<Step> Steps { get; set; } = new();

    private Job EditJob { get; set; } = new();

    private List<SqlConnectionInfo>? SqlConnections { get; set; }
    private List<AnalysisServicesConnectionInfo>? AsConnections { get; set; }
    private List<PipelineClient>? PipelineClients { get; set; }
    private List<AppRegistration>? AppRegistrations { get; set; }
    private List<FunctionApp>? FunctionApps { get; set; }

    private bool DescriptionOpen { get; set; }

    protected override async Task OnInitializedAsync()
    {
        using var context = DbFactory.CreateDbContext();
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
            .Include(job => job.JobConcurrencies)
            .FirstAsync(job => job.JobId == Id);
        Job.JobParameters = Job.JobParameters.OrderBy(p => p.ParameterName).ToList();
        EditJob.JobName = Job.JobName;
        EditJob.JobDescription = Job.JobDescription;
        EditJob.OvertimeNotificationLimitMinutes = Job.OvertimeNotificationLimitMinutes;
        Steps = await context.Steps
            .AsNoTrackingWithIdentityResolution()
            .Include(step => step.Dependencies)
            .Include(step => step.Sources)
            .Include(step => step.Targets)
            .Include(step => step.Tags)
            .Include(step => (step as ParameterizedStep)!.StepParameters)
            .Include(step => step.ExecutionConditionParameters)
            .Where(step => step.JobId == Job.JobId)
            .ToListAsync();
        SortSteps();
        Jobs = await context.Jobs.OrderBy(j => j.JobName).ToListAsync();
    }

    private void SortSteps()
    {
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

    private async Task UpdateJob()
    {
        try
        {
            using var context = DbFactory.CreateDbContext();
            Job.JobName = EditJob.JobName;
            Job.JobDescription = EditJob.JobDescription;
            Job.OvertimeNotificationLimitMinutes = EditJob.OvertimeNotificationLimitMinutes;
            context.Attach(Job).State = EntityState.Modified;
            await context.SaveChangesAsync();
            Messenger.AddInformation("Job settings saved successfully");
        }
        catch (DbUpdateConcurrencyException)
        {
            Messenger.AddError("Concurrency error", "The job was modified outside of this session. Reload the page to view the most recent values.");
        }
        catch (Exception ex)
        {
            Messenger.AddError("Error updating job", ex.Message);
        }
    }

    private async Task DeleteJob()
    {
        try
        {
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
            using var context = DbFactory.CreateDbContext();
            context.Attach(Job);
            Job.IsEnabled = !Job.IsEnabled;
            await context.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            Messenger.AddError("Error toggling job", ex.Message);
        }
    }

    private async Task ToggleDependencyMode(bool value)
    {
        try
        {
            using var context = DbFactory.CreateDbContext();
            context.Attach(Job);
            Job.UseDependencyMode = value;
            await context.SaveChangesAsync();
            SortSteps();
        }
        catch (Exception ex)
        {
            Messenger.AddError("Error toggling mode", ex.Message);
        }
    }

    private async Task ToggleStopOnFirstError(ChangeEventArgs args)
    {
        try
        {
            using var context = DbFactory.CreateDbContext();
            context.Attach(Job);
            Job.StopOnFirstError = !Job.StopOnFirstError;
            await context.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            Messenger.AddError("Error toggling setting", ex.Message);
        }
    }

}