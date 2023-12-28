using Biflow.DataAccess;
using Biflow.DataAccess.Models;
using Biflow.Ui.Core;
using Biflow.Ui.Shared;
using Biflow.Ui.Shared.JobDetails;
using Havit.Blazor.Components.Web;
using Havit.Blazor.Components.Web.Bootstrap;
using MediatR;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.EntityFrameworkCore;

namespace Biflow.Ui.Pages;

[Route("/jobs")]
public partial class Jobs : ComponentBase
{
    [Inject] private IDbContextFactory<AppDbContext> DbFactory { get; set; } = null!;
    [Inject] private JobDuplicatorFactory JobDuplicatorFactory { get; set; } = null!;
    [Inject] private IHxMessengerService Messenger { get; set; } = null!;
    [Inject] private AuthenticationStateProvider AuthenticationStateProvider { get; set; } = null!;
    [Inject] private NavigationManager NavigationManager { get; set; } = null!;
    [Inject] private IHxMessageBoxService Confirmer { get; set; } = null!;
    [Inject] private IHttpContextAccessor HttpContextAccessor { get; set; } = null!;
    [Inject] private IMediator Mediator { get; set; } = null!;

    [CascadingParameter] public UserState UserState { get; set; } = new();

    private readonly HashSet<ExecutionStatus> statusFilter = [];

    private List<Job>? jobs;    
    private List<JobCategory>? categories;
    private Dictionary<Guid, Execution>? lastExecutions;
    private bool isLoading = false;
    private JobCategoryEditModal? categoryEditModal;
    private JobEditModal? jobEditModal;
    private ExecuteModal? executeModal;
    private string jobNameFilter = "";
    private ExecutionStartResponse? lastStartedExecutionResponse;

    protected override async Task OnInitializedAsync()
    {
        await LoadData();
    }

    private async Task LoadData()
    {
        isLoading = true;
        using var context = await Task.Run(DbFactory.CreateDbContext);
        jobs = await context.Jobs
            .AsNoTrackingWithIdentityResolution()
            .Include(job => job.Schedules)
            .Include(job => job.Category)
            .OrderBy(job => job.JobName)
            .ToListAsync();

        // For admins and editors, show all available job categories.
        var authState = await AuthenticationStateProvider.GetAuthenticationStateAsync();
        if (authState.User.IsInRole(Roles.Admin) || authState.User.IsInRole(Roles.Editor))
        {
            categories = await context.JobCategories
                .AsNoTrackingWithIdentityResolution()
                .OrderBy(c => c.CategoryName)
                .ToListAsync();
        }
        // For other users, only show categories for jobs they are authorized to see.
        else
        {
            categories = jobs
                .Select(j => j.Category)
                .Where(c => c is not null)
                .Cast<JobCategory>()
                .DistinctBy(c => c.CategoryId)
                .ToList();
        }

        StateHasChanged(); // Render/publish results so far (jobs),
        await LoadLastExecutions(context); // Load last execution status for jobs (possibly heavy operation).
        isLoading = false;
    }

    private async Task LoadLastExecutions(AppDbContext context)
    {
        ArgumentNullException.ThrowIfNull(jobs);
        // Get each job's last execution.
        var lastExecutions = await context.Executions
            .AsNoTrackingWithIdentityResolution()
            .Where(execution => jobs.Select(job => job.JobId).Contains(execution.JobId) && execution.StartedOn != null)
            .Select(execution => execution.JobId)
            .Distinct()
            .Select(key => new
            {
                Key = key,
                Execution = context.Executions.Where(execution => execution.JobId == key).OrderByDescending(e => e.CreatedOn).First()
            })
            .ToListAsync();

        this.lastExecutions = lastExecutions.ToDictionary(e => e.Key, e => e.Execution);
        StateHasChanged();
    }

    // Helper method for Dictionary TryGet access
    private Execution? GetLastExecution(Job job)
    {
        Execution? execution = null;
        lastExecutions?.TryGetValue(job.JobId, out execution);
        return execution;
    }

    private static DateTime? GetNextStartTime(Job job)
    {
        var dateTimes = job.Schedules.Where(s => s.IsEnabled).Select(s => s.NextFireTimes().FirstOrDefault());
        return dateTimes.Any() ? dateTimes.Min() : null;
    }

    private async Task ToggleEnabled(ChangeEventArgs args, Job job)
    {
        bool value = (bool)args.Value!;
        try
        {
            using var context = DbFactory.CreateDbContext();
            context.Attach(job);
            job.IsEnabled = value;
            await context.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            Messenger.AddError("Error toggling job", ex.Message);
        }
    }

    private async Task CopyJob(Job job)
    {
        try
        {
            var duplicator = await JobDuplicatorFactory.CreateAsync(job.JobId);
            duplicator.Job.JobName = $"{duplicator.Job.JobName} – Copy";
            var createdJob = await duplicator.SaveJobAsync();
            createdJob.Schedules = new List<Schedule>();
            jobs?.Add(createdJob);
            jobs = jobs?.OrderBy(job_ => job_.JobName).ToList();
        }
        catch (Exception ex)
        {
            Messenger.AddError("Error copying job", ex.Message);
        }
    }

    private async Task DeleteJob(Job job)
    {
        if (!await Confirmer.ConfirmAsync("Delete job", $"Are you sure you want to delete \"{job.JobName}\"?"))
        {
            return;
        }
        using (var context = await DbFactory.CreateDbContextAsync())
        {
            var executingSteps = await context.JobSteps
                .Where(s => s.JobToExecuteId == job.JobId)
                .Include(s => s.Job)
                .OrderBy(s => s.Job.JobName)
                .ThenBy(s => s.StepName)
                .ToListAsync();
            if (executingSteps.Count != 0)
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
        try
        {
            await Mediator.Send(new DeleteJobRequest(job.JobId));
            jobs?.Remove(job);
        }
        catch (Exception ex)
        {
            Messenger.AddError("Error deleting job", ex.Message);
        }
    }

    private void GoToExecutionDetails(Guid executionId)
    {
        NavigationManager.NavigateTo($"executions/{executionId}/list");
    }

    private void OnJobSubmitted(Job job)
    {
        var remove = jobs?.FirstOrDefault(j => j.JobId == job.JobId);
        if (remove is not null)
        {
            job.Schedules = remove.Schedules;
            jobs?.Remove(remove);
        }
        else
        {
            job.Schedules = new List<Schedule>();
        }
        jobs?.Add(job);
        jobs?.Sort((j1, j2) => j1.JobName.CompareTo(j2.JobName));
    }

    private void OnCategorySubmitted(JobCategory category)
    {
        var remove = categories?.FirstOrDefault(c => c.CategoryId == category.CategoryId);
        if (remove is not null)
        {
            categories?.Remove(remove);
        }
        categories?.Add(category);
        categories?.Sort((c1, c2) => c1.CategoryName.CompareTo(c2.CategoryName));
    }

    private async Task DeleteCategoryAsync(JobCategory category)
    {
        if(!await Confirmer.ConfirmAsync("Delete category", $"Are you sure you want to delete \"{category.CategoryName}\"?"))
        {
            return;
        }
        try
        {
            using var context = DbFactory.CreateDbContext();
            context.JobCategories.Remove(category);
            await context.SaveChangesAsync();
            categories?.Remove(category);
            foreach (var job in jobs?.Where(t => t.CategoryId == category.CategoryId) ?? Enumerable.Empty<Job>())
            {
                job.CategoryId = null;
                job.Category = null;
            }
        }
        catch (Exception ex)
        {
            Messenger.AddError("Error deleting category", ex.Message);
        }
    }

    private void OnExecutionStarted(ExecutionStartResponse response)
    {
        lastStartedExecutionResponse = response;
    }

    private void ExpandAll()
    {
        foreach (var category in categories ?? Enumerable.Empty<JobCategory>())
        {
            var state = UserState.JobCategoryExpandStatuses.GetOrCreate(category.CategoryId);
            state.IsExpanded = true;
        }
        var noCategoryState = UserState.JobCategoryExpandStatuses.GetOrCreate(Guid.Empty);
        noCategoryState.IsExpanded = true;
    }

    private void CollapseAll()
    {
        foreach (var category in categories ?? Enumerable.Empty<JobCategory>())
        {
            var state = UserState.JobCategoryExpandStatuses.GetOrCreate(category.CategoryId);
            state.IsExpanded = false;
        }
        var noCategoryState = UserState.JobCategoryExpandStatuses.GetOrCreate(Guid.Empty);
        noCategoryState.IsExpanded = false;
    }

    private class ExpandStatus { public bool IsExpanded { get; set; } = true; }
}
