using Biflow.Ui.Shared;
using Biflow.Ui.Shared.JobDetails;

namespace Biflow.Ui.Pages;

[Route("/jobs")]
public partial class Jobs : ComponentBase, IDisposable
{
    [Inject] private IDbContextFactory<AppDbContext> DbFactory { get; set; } = null!;
    [Inject] private JobDuplicatorFactory JobDuplicatorFactory { get; set; } = null!;
    [Inject] private ToasterService Toaster { get; set; } = null!;
    [Inject] private NavigationManager NavigationManager { get; set; } = null!;
    [Inject] private IHxMessageBoxService Confirmer { get; set; } = null!;
    [Inject] private IMediator Mediator { get; set; } = null!;

    [CascadingParameter] public Task<AuthenticationState>? AuthenticationState { get; set; }
    [CascadingParameter] public UserState UserState { get; set; } = new();

    private readonly HashSet<ExecutionStatus> statusFilter = [];
    private readonly CancellationTokenSource cts = new();

    private List<Job>? jobs;    
    private List<JobCategory>? categories;
    private Dictionary<Guid, Execution>? lastExecutions;
    private List<StepProjection> steps = [];
    private bool isLoading = false;
    private JobCategoryEditModal? categoryEditModal;
    private JobEditModal? jobEditModal;
    private ExecuteModal? executeModal;
    private string jobNameFilter = "";
    private string stepNameFilter = "";
    private StateFilter stateFilter = StateFilter.All;

    private IEnumerable<ListItem> FilteredJobs => jobs?
        .Where(j => stateFilter switch { StateFilter.Enabled => j.IsEnabled, StateFilter.Disabled => !j.IsEnabled, _ => true })
        .Where(j => string.IsNullOrEmpty(jobNameFilter) || j.JobName.ContainsIgnoreCase(jobNameFilter))
        .Where(j => string.IsNullOrEmpty(stepNameFilter) || steps.Any(s => s.JobId == j.JobId && (s.StepName?.ContainsIgnoreCase(stepNameFilter) ?? false)))
        .Select(j => new ListItem(j, lastExecutions?.GetValueOrDefault(j.JobId), GetNextStartTime(j)))
        .Where(j => statusFilter.Count == 0 || j.LastExecution is not null && statusFilter.Contains(j.LastExecution.ExecutionStatus))
        ?? [];

    private enum StateFilter { All, Enabled, Disabled }

    private record ListItem(Job Job, Execution? LastExecution, DateTime? NextExecution);

    protected override async Task OnInitializedAsync()
    {
        ArgumentNullException.ThrowIfNull(AuthenticationState);
        var authState = await AuthenticationState;
        var user = authState.User;
        await LoadDataAsync();
    }

    private async Task LoadDataAsync()
    {
        isLoading = true;
        await Task.WhenAll(
            LoadJobsAsync(),
            LoadLastExecutionsAsync(),
            LoadStepsAsync());
        isLoading = false;
        StateHasChanged();
    }

    private async Task LoadJobsAsync()
    {
        using var context = await Task.Run(DbFactory.CreateDbContext);
        jobs = await context.Jobs
            .AsNoTrackingWithIdentityResolution()
            .Include(job => job.Schedules)
            .Include(job => job.Category)
            .OrderBy(job => job.JobName)
            .ToListAsync(cts.Token);

        // For admins and editors, show all available job categories.
        ArgumentNullException.ThrowIfNull(AuthenticationState);
        var authState = await AuthenticationState;
        if (authState.User.IsInRole(Roles.Admin) || authState.User.IsInRole(Roles.Editor))
        {
            categories = await context.JobCategories
                .AsNoTrackingWithIdentityResolution()
                .OrderBy(c => c.CategoryName)
                .ToListAsync(cts.Token);
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
        StateHasChanged();
    }

    private async Task LoadLastExecutionsAsync()
    {
        // Get each job's last execution.
        using var context = await Task.Run(DbFactory.CreateDbContext);
        var lastExecutions = await context.Executions
            .AsNoTrackingWithIdentityResolution()
            .Where(execution => context.Jobs.Any(j => j.JobId == execution.JobId) && execution.StartedOn != null)
            .Select(execution => execution.JobId)
            .Distinct()
            .Select(key => new
            {
                Key = key,
                Execution = context.Executions.Where(execution => execution.JobId == key).OrderByDescending(e => e.CreatedOn).First()
            })
            .ToListAsync(cts.Token);
        this.lastExecutions = lastExecutions.ToDictionary(e => e.Key, e => e.Execution);
        StateHasChanged();
    }

    private async Task LoadStepsAsync()
    {
        using var context = await Task.Run(DbFactory.CreateDbContext);
        steps = await context.Steps
            .AsNoTracking()
            .Select(s => new StepProjection(s.JobId, s.StepName))
            .ToListAsync(cts.Token);
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
            await Mediator.SendAsync(new ToggleJobCommand(job.JobId, value));
            job.IsEnabled = value;
        }
        catch (Exception ex)
        {
            Toaster.AddError("Error toggling job", ex.Message);
        }
    }

    private async Task CopyJob(Job job)
    {
        try
        {
            using var duplicator = await JobDuplicatorFactory.CreateAsync(job.JobId);
            duplicator.Job.JobName = $"{duplicator.Job.JobName} – Copy";
            var createdJob = await duplicator.SaveJobAsync();
            jobs?.Add(createdJob);
            jobs = jobs?.OrderBy(job_ => job_.JobName).ToList();
        }
        catch (Exception ex)
        {
            Toaster.AddError("Error copying job", ex.Message);
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
            await Mediator.SendAsync(new DeleteJobCommand(job.JobId));
            jobs?.Remove(job);
        }
        catch (Exception ex)
        {
            Toaster.AddError("Error deleting job", ex.Message);
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
            job.Schedules.AddRange(remove.Schedules);
            jobs?.Remove(remove);
        }
        jobs?.Add(job);
        jobs?.SortBy(x => x.JobName);
    }

    private void OnCategorySubmitted(JobCategory category)
    {
        var remove = categories?.FirstOrDefault(c => c.CategoryId == category.CategoryId);
        if (remove is not null)
        {
            categories?.Remove(remove);
        }
        categories?.Add(category);
        categories?.SortBy(x => x.CategoryName);
    }

    private async Task DeleteCategoryAsync(JobCategory category)
    {
        if(!await Confirmer.ConfirmAsync("Delete category", $"Are you sure you want to delete \"{category.CategoryName}\"?"))
        {
            return;
        }
        try
        {
            await Mediator.SendAsync(new DeleteJobCategoryCommand(category.CategoryId));
            categories?.Remove(category);
            foreach (var job in jobs?.Where(t => t.CategoryId == category.CategoryId) ?? [])
            {
                job.CategoryId = null;
                job.Category = null;
            }
        }
        catch (Exception ex)
        {
            Toaster.AddError("Error deleting category", ex.Message);
        }
    }

    public void Dispose()
    {
        cts.Cancel();
        cts.Dispose();
    }

    private class ExpandStatus { public bool IsExpanded { get; set; } = true; }

    private record StepProjection(Guid JobId, string? StepName);
}
