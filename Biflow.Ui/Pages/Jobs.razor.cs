using Biflow.Ui.Shared;
using Biflow.Ui.Shared.Executions;
using Biflow.Ui.Shared.JobDetails;
using Biflow.Ui.Shared.JobsBatchEdit;
using Microsoft.JSInterop;
using System.Text.Json;

namespace Biflow.Ui.Pages;

[Route("/jobs")]
public partial class Jobs(
    IDbContextFactory<AppDbContext> dbContextFactory,
    JobDuplicatorFactory jobDuplicatorFactory,
    ToasterService toaster,
    IHxMessageBoxService confirmer,
    IMediator mediator,
    IJSRuntime js) : ComponentBase, IDisposable
{
    [CascadingParameter] public Task<AuthenticationState>? AuthenticationState { get; set; }
    [CascadingParameter] public UserState UserState { get; set; } = new();
    [CascadingParameter] public ExecuteMultipleModal ExecuteMultipleModal { get; set; } = null!;

    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };
    private readonly IDbContextFactory<AppDbContext> _dbContextFactory = dbContextFactory;
    private readonly JobDuplicatorFactory _jobDuplicatorFactory = jobDuplicatorFactory;
    private readonly ToasterService _toaster = toaster;
    private readonly IHxMessageBoxService _confirmer = confirmer;
    private readonly IMediator _mediator = mediator;
    private readonly IJSRuntime _js = js;
    private readonly CancellationTokenSource _cts = new();

    private List<Job>? _jobs;    
    private Dictionary<Guid, Execution>? _lastExecutions;
    private List<StepProjection> _steps = [];
    private bool _isLoading;
    private JobEditModal? _jobEditModal;
    private ExecuteModal? _executeModal;
    private JobsBatchEditTagsModal? _jobsBatchEditTagsModal;
    private Paginator<ListItem>? _paginator;
    private HashSet<Job> _selectedJobs = [];
    private JobHistoryOffcanvas? _jobHistoryOffcanvas;

    private record ListItem(Job Job, Execution? LastExecution, Schedule? NextSchedule, DateTime? NextExecution);

    protected override Task OnInitializedAsync()
    {
        return LoadDataAsync();
    }

    private IEnumerable<ListItem> GetListItems()
    {
        var jobNameFilter = UserState.Jobs.JobNameFilter;
        var stepNameFilter = UserState.Jobs.StepNameFilter;
        var stateFilter = UserState.Jobs.StateFilter;
        var sortMode = UserState.Jobs.SortMode;
        var statusFilter = UserState.Jobs.StatusFilter;
        var tagFilter = UserState.Jobs.TagFilter;

        var items = _jobs?
            .Where(j => stateFilter switch { StateFilter.Enabled => j.IsEnabled, StateFilter.Disabled => !j.IsEnabled, _ => true })
            .Where(j => string.IsNullOrEmpty(jobNameFilter) || j.JobName.ContainsIgnoreCase(jobNameFilter))
            .Where(j => string.IsNullOrEmpty(stepNameFilter) || _steps.Any(s => s.JobId == j.JobId && (s.StepName?.ContainsIgnoreCase(stepNameFilter) ?? false)))
            .Select(j =>
            {
                var schedule = GetNextSchedule(j);
                var nextExecution = schedule?.NextFireTimes().FirstOrDefault();
                return new ListItem(j, _lastExecutions?.GetValueOrDefault(j.JobId), schedule, nextExecution);
            })
            .Where(j => statusFilter.Count == 0 || j.LastExecution is not null && statusFilter.Contains(j.LastExecution.ExecutionStatus))
            .Where(j => tagFilter.Count == 0 || j.Job.Tags.Any(t1 => tagFilter.Any(t2 => t1.TagId == t2.TagId)))
            ?? [];
        return sortMode switch
        {
            JobSortMode.Pinned => items.OrderBy(i => !i.Job.IsPinned).ThenBy(i => i.Job.JobName),
            JobSortMode.NameAsc => items.OrderBy(i => i.Job.JobName),
            JobSortMode.NameDesc => items.OrderByDescending(i => i.Job.JobName),
            JobSortMode.LastExecAsc => items.OrderBy(i => i.LastExecution?.StartedOn is null).ThenBy(i => i.LastExecution?.StartedOn?.LocalDateTime),
            JobSortMode.LastExecDesc => items.OrderBy(i => i.LastExecution?.StartedOn is null).ThenByDescending(i => i.LastExecution?.StartedOn?.LocalDateTime),
            JobSortMode.NextExecAsc => items.OrderBy(i => i.NextExecution is null).ThenBy(i => i.NextExecution),
            JobSortMode.NextExecDesc => items.OrderBy(i => i.NextExecution is null).ThenByDescending(i => i.NextExecution),
            _ => items
        };
    }

    private async Task LoadDataAsync()
    {
        _isLoading = true;
        await Task.WhenAll(
            LoadJobsAsync(),
            LoadLastExecutionsAsync(),
            LoadStepsAsync());
        _isLoading = false;
        StateHasChanged();
    }

    private async Task LoadJobsAsync()
    {
        await using var context = await _dbContextFactory.CreateDbContextAsync();
        _jobs = await context.Jobs
            .AsNoTrackingWithIdentityResolution()
            .Include(job => job.Tags)
            .Include(job => job.Schedules)
            .OrderBy(job => job.JobName)
            .ToListAsync(_cts.Token);
        StateHasChanged();
    }

    private async Task LoadLastExecutionsAsync()
    {
        // Get each job's last execution.
        await using var context = await _dbContextFactory.CreateDbContextAsync();
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
            .ToListAsync(_cts.Token);
        _lastExecutions = lastExecutions.ToDictionary(e => e.Key, e => e.Execution);
        StateHasChanged();
    }

    private async Task LoadStepsAsync()
    {
        await using var context = await _dbContextFactory.CreateDbContextAsync();
        _steps = await context.Steps
            .AsNoTracking()
            .Select(s => new StepProjection(s.JobId, s.StepName))
            .ToListAsync(_cts.Token);
    }

    private static Schedule? GetNextSchedule(Job job)
    {
        var schedule = job.Schedules
            .Where(s => s.IsEnabled)
            .Select(s => new { Schedule = s, NextFireTime = s.NextFireTimes().FirstOrDefault() })
            .MinBy(s => s.NextFireTime);
        return schedule?.Schedule;
    }

    private async Task TogglePinned(Job job)
    {
        try
        {
            await _mediator.SendAsync(new ToggleJobPinnedCommand(job.JobId, !job.IsPinned));
            job.IsPinned = !job.IsPinned;
            var message = job.IsPinned ? "Job pinned" : "Job unpinned";
            _toaster.AddSuccess(message, 2500);
        }
        catch (Exception ex)
        {
            _toaster.AddError("Error pinning/unpinning job", ex.Message);
        }
    }

    private async Task ToggleEnabled(ChangeEventArgs args, Job job)
    {
        var value = (bool)args.Value!;
        try
        {
            await _mediator.SendAsync(new ToggleJobCommand(job.JobId, value));
            job.IsEnabled = value;
            var message = value ? "Job enabled" : "Job disabled";
            _toaster.AddSuccess(message, 2500);
        }
        catch (Exception ex)
        {
            _toaster.AddError("Error toggling job", ex.Message);
        }
    }

    private async Task ToggleSelectedEnabled(bool value)
    {
        try
        {
            foreach (var job in _selectedJobs)
            {
                await _mediator.SendAsync(new ToggleJobCommand(job.JobId, value));
                job.IsEnabled = value;
            }

        }
        catch (Exception ex)
        {
            _toaster.AddError("Error toggling job", ex.Message);
        }
    }

    private void OnJobsSubmit(IEnumerable<Job> jobs)
    {
        foreach (var job in jobs.ToArray())
        {
            var index = _jobs?.FindIndex(j => j.JobId == job.JobId);
            if (index is { } i and >= 0)
            {
                _jobs?.RemoveAt(i);
                _jobs?.Insert(i, job);
            }
            else
            {
                _jobs?.Add(job);
            }
        }
        _selectedJobs = jobs.ToHashSet();
    }

    private async Task CopyJob(Job job)
    {
        try
        {
            using var duplicator = await _jobDuplicatorFactory.CreateAsync(job.JobId);
            duplicator.Job.JobName = $"{duplicator.Job.JobName} – Copy";
            var createdJob = await duplicator.SaveJobAsync();
            _jobs?.Add(createdJob);
            _jobs = _jobs?.OrderBy(j => j.JobName).ToList();
        }
        catch (Exception ex)
        {
            _toaster.AddError("Error copying job", ex.Message);
        }
    }

    private async Task DeleteJob(Job job)
    {
        if (!await _confirmer.ConfirmAsync("Delete job", $"Are you sure you want to delete \"{job.JobName}\"?"))
        {
            return;
        }

        await using (var context = await _dbContextFactory.CreateDbContextAsync())
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
                var confirmResult = await _confirmer.ConfirmAsync("", message);
                if (!confirmResult)
                {
                    return;
                }
            }
        }
        try
        {
            await _mediator.SendAsync(new DeleteJobCommand(job.JobId));
            _jobs?.Remove(job);
        }
        catch (Exception ex)
        {
            _toaster.AddError("Error deleting job", ex.Message);
        }
    }

    private async Task DeleteSelectedJobsAsync()
    {
        if (!await _confirmer.ConfirmAsync("Delete selected jobs", $"Delete {_selectedJobs.Count} job(s)?"))
        {
            return;
        }
        var jobIds = _selectedJobs.Select(j => j.JobId).ToArray();
        await using (var context = await _dbContextFactory.CreateDbContextAsync())
        {
            var executingSteps = await context.JobSteps
                .Where(s => s.JobToExecuteId != null && jobIds.Contains((Guid)s.JobToExecuteId))
                .Include(s => s.Job)
                .OrderBy(s => s.Job.JobName)
                .ThenBy(s => s.StepName)
                .ToListAsync();
            if (executingSteps.Count != 0)
            {
                var steps = string.Join("\n", executingSteps.Select(s => $"– {s.Job.JobName} – {s.StepName}"));
                var message = $"""
                    Selected jobs have one or more referencing steps that execute them:
                    {steps}
                    Removing the jobs will also remove these steps. Delete anyway?
                    """;
                var confirmResult = await _confirmer.ConfirmAsync("", message);
                if (!confirmResult)
                {
                    return;
                }
            }
        }
        try
        {
            foreach (var job in _selectedJobs)
            {
                await _mediator.SendAsync(new DeleteJobCommand(job.JobId));
                _jobs?.Remove(job);
            }
        }
        catch (Exception ex)
        {
            _toaster.AddError("Error deleting jobs", ex.Message);
        }
        _selectedJobs.Clear();
    }

    private void ToggleJobsSelected(IEnumerable<Job> jobs, bool value)
    {
        if (value)
        {
            var jobsToAdd = jobs.Where(j => !_selectedJobs.Contains(j));
            foreach (var j in jobsToAdd) _selectedJobs.Add(j);
        }
        else
        {
            _selectedJobs.Clear();
        }
    }

    private async Task ValidateAllStepDependenciesAsync()
    {
        try
        {
            await using var context = await _dbContextFactory.CreateDbContextAsync();
            var steps = await context.Steps
                .Select(s => new ValidationStep(s.Job.JobName, s.StepId, s.StepName, s.Dependencies.Select(d => d.DependantOnStepId).ToArray()))
                .ToListAsync();
            var comparer = new TopologicalComparer<ValidationStep, Guid>(
                items: steps,
                keySelector: s => s?.StepId ?? Guid.Empty,
                dependenciesSelector: s => s.Dependencies);
            steps.Sort(comparer);
            _toaster.AddSuccess("Validation successful");
        }
        catch (CyclicDependencyException<ValidationStep> ex)
        {
            var cycles = ex.CyclicObjects.Select(c => c.Select(s => new { s.JobName, s.StepName }));
            var message = JsonSerializer.Serialize(cycles, JsonOptions);
            _ = _js.InvokeVoidAsync("console.log", message).AsTask();
            _toaster.AddError("Cyclic dependencies detected", "See browser console for detailed output.");
        }
        catch (Exception ex)
        {
            _toaster.AddError("Error validating dependencies", ex.Message);
        }
    }

    private void OnJobSubmitted(Job job)
    {
        var remove = _jobs?.FirstOrDefault(j => j.JobId == job.JobId);
        if (remove is not null)
        {
            job.Schedules.AddRange(remove.Schedules);
            _jobs?.Remove(remove);
        }
        _jobs?.Add(job);
        _jobs?.SortBy(x => x.JobName);
    }

    public void Dispose()
    {
        _cts.Cancel();
        _cts.Dispose();
    }
    
    private record StepProjection(Guid JobId, string? StepName);

    private record ValidationStep(string JobName, Guid StepId, string? StepName, Guid[] Dependencies);
}
