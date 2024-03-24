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

    private readonly CancellationTokenSource cts = new();

    private List<Job>? jobs;    
    private Dictionary<Guid, Execution>? lastExecutions;
    private List<StepProjection> steps = [];
    private bool isLoading = false;
    private JobEditModal? jobEditModal;
    private ExecuteModal? executeModal;
    private Paginator<ListItem>? paginator;
    private ListItem[] listItems = [];

    private record ListItem(Job Job, Execution? LastExecution, DateTime? NextExecution);

    protected override Task OnInitializedAsync()
    {
        return LoadDataAsync();
    }

    private void UpdateListItems()
    {
        var jobNameFilter = UserState.Jobs.JobNameFilter;
        var stepNameFilter = UserState.Jobs.StepNameFilter;
        var stateFilter = UserState.Jobs.StateFilter;
        var sortMode = UserState.Jobs.SortMode;
        var statusFilter = UserState.Jobs.StatusFilter;

        var items = jobs?
            .Where(j => stateFilter switch { StateFilter.Enabled => j.IsEnabled, StateFilter.Disabled => !j.IsEnabled, _ => true })
            .Where(j => string.IsNullOrEmpty(jobNameFilter) || j.JobName.ContainsIgnoreCase(jobNameFilter))
            .Where(j => string.IsNullOrEmpty(stepNameFilter) || steps.Any(s => s.JobId == j.JobId && (s.StepName?.ContainsIgnoreCase(stepNameFilter) ?? false)))
            .Select(j => new ListItem(j, lastExecutions?.GetValueOrDefault(j.JobId), GetNextStartTime(j)))
            .Where(j => statusFilter.Count == 0 || j.LastExecution is not null && statusFilter.Contains(j.LastExecution.ExecutionStatus))
            ?? [];
        items = sortMode switch
        {
            JobSortMode.NameAsc => items.OrderBy(i => i.Job.JobName),
            JobSortMode.NameDesc => items.OrderByDescending(i => i.Job.JobName),
            JobSortMode.LastExecAsc => items.OrderBy(i => i.LastExecution?.StartedOn is null).ThenBy(i => i.LastExecution?.StartedOn?.LocalDateTime),
            JobSortMode.LastExecDesc => items.OrderBy(i => i.LastExecution?.StartedOn is null).ThenByDescending(i => i.LastExecution?.StartedOn?.LocalDateTime),
            JobSortMode.NextExecAsc => items.OrderBy(i => i.NextExecution is null).ThenBy(i => i.NextExecution),
            JobSortMode.NextExecDesc => items.OrderBy(i => i.NextExecution is null).ThenByDescending(i => i.NextExecution),
            _ => items
        };
        listItems = items.ToArray();
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
            .OrderBy(job => job.JobName)
            .ToListAsync(cts.Token);
        UpdateListItems();
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
        UpdateListItems();
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
            UpdateListItems();
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
            UpdateListItems();
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
            UpdateListItems();
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
        UpdateListItems();
    }

    public void Dispose()
    {
        cts.Cancel();
        cts.Dispose();
    }

    private class ExpandStatus { public bool IsExpanded { get; set; } = true; }

    private record StepProjection(Guid JobId, string? StepName);
}
