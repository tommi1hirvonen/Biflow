using Biflow.DataAccess;
using Biflow.DataAccess.Models;
using Biflow.Ui.Core;
using Biflow.Ui.Shared;
using Biflow.Ui.Shared.Executions;
using Havit.Blazor.Components.Web;
using Havit.Blazor.Components.Web.Bootstrap;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.EntityFrameworkCore;
using System.Runtime.CompilerServices;

namespace Biflow.Ui.Pages;

public partial class Jobs : ComponentBase
{
    [Inject] private IDbContextFactory<BiflowContext> DbFactory { get; set; } = null!;
    
    [Inject] private ISchedulerService SchedulerService { get; set; } = null!;
    
    [Inject] private DbHelperService DbHelperService { get; set; } = null!;
    
    [Inject] private IHttpContextAccessor HttpContextAccessor { get; set; } = null!;
    
    [Inject] private IHxMessengerService Messenger { get; set; } = null!;
    
    [Inject] private AuthenticationStateProvider AuthenticationStateProvider { get; set; } = null!;

    private List<Job>? Jobs_ { get; set; }
    
    private List<JobCategory>? Categories { get; set; }
    
    private Dictionary<Guid, Execution>? LastExecutions { get; set; }

    private bool IsLoading { get; set; } = false;

    private JobExecutionDetailsModal? JobExecutionModal { get; set; }
    
    private Guid SelectedJobExecutionId { get; set; }

    private JobCategoryEditModal? CategoryEditModal { get; set; }
    
    private JobEditModal? JobEditModal { get; set; }

    private string JobNameFilter { get; set; } = "";
    
    private HashSet<ExecutionStatus> StatusFilter { get; } = new();

    private ConditionalWeakTable<JobCategory, ExpandStatus> CategoryExpandStatuses { get; } = new();
    
    private ExpandStatus NoCategoryExpanded { get; } = new();

    protected override async Task OnInitializedAsync()
    {
        await LoadData();
    }

    private async Task LoadData()
    {
        IsLoading = true;
        using var context = await Task.Run<BiflowContext>(DbFactory.CreateDbContext);
        Jobs_ = await context.Jobs
            .AsNoTrackingWithIdentityResolution()
            .Include(job => job.Schedules)
            .Include(job => job.Category)
            .OrderBy(job => job.JobName)
            .ToListAsync();

        // For admins and editors, show all available job categories.
        var authState = await AuthenticationStateProvider.GetAuthenticationStateAsync();
        if (authState.User.IsInRole("Admin") || authState.User.IsInRole("Editor"))
        {
            Categories = await context.JobCategories
                .AsNoTrackingWithIdentityResolution()
                .OrderBy(c => c.CategoryName)
                .ToListAsync();
        }
        // For other users, only show categories for jobs they are authorized to see.
        else
        {
            Categories = Jobs_
                .Select(j => j.Category)
                .Where(c => c is not null)
                .Cast<JobCategory>()
                .DistinctBy(c => c.CategoryId)
                .ToList();
        }

        StateHasChanged(); // Render/publish results so far (jobs),
        await LoadLastExecutions(context); // Load last execution status for jobs (possibly heavy operation).
        IsLoading = false;
    }

    private async Task LoadLastExecutions(BiflowContext context)
    {
        ArgumentNullException.ThrowIfNull(Jobs_);
        // Get each job's last execution.
        var lastExecutions = await context.Executions
            .AsNoTrackingWithIdentityResolution()
            .Where(execution => Jobs_.Select(job => job.JobId).Contains(execution.JobId ?? Guid.Empty) && execution.StartDateTime != null)
            .Select(execution => execution.JobId)
            .Distinct()
            .Select(key => new
            {
                Key = key,
                Execution = context.Executions.Where(execution => execution.JobId == key).OrderByDescending(e => e.CreatedDateTime).First()
            })
            .ToListAsync();

        LastExecutions = lastExecutions.ToDictionary(e => e.Key ?? Guid.Empty, e => e.Execution);
        StateHasChanged();
    }

    // Helper method for Dictionary TryGet access
    private Execution? GetLastExecution(Job job)
    {
        Execution? execution = null;
        LastExecutions?.TryGetValue(job.JobId, out execution);
        return execution;
    }

    private static DateTime? GetNextStartTime(Job job)
    {
        var dateTimes = job.Schedules.Where(s => s.IsEnabled).Select(s => s.GetNextFireTime());
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
            string user = HttpContextAccessor.HttpContext?.User?.Identity?.Name
                ?? throw new ArgumentNullException(nameof(user), "Error getting username from HttpContext");
            using var context = DbFactory.CreateDbContext();
            Guid createdJobId = await DbHelperService.JobCopyAsync(job.JobId, user);
            var createdJob = await context.Jobs
            .Include(j => j.Schedules)
            .Include(j => j.Category)
            .FirstAsync(j => j.JobId == createdJobId);
            Jobs_?.Add(createdJob);
            Jobs_ = Jobs_?.OrderBy(job_ => job_.JobName).ToList();
        }
        catch (Exception ex)
        {
            Messenger.AddError("Error copying job", ex.Message);
        }
    }

    private async Task DeleteJob(Job job)
    {
        try
        {
            using var context = await DbFactory.CreateDbContextAsync();
            await context.Jobs
                .Where(j => j.JobId == job.JobId)
                .ExecuteDeleteAsync();
            await SchedulerService.DeleteJobAsync(job);
            Jobs_?.Remove(job);
        }
        catch (Exception ex)
        {
            Messenger.AddError("Error deleting job", ex.Message);
        }
    }

    private async Task OpenJobExecutionModal(Guid executionId)
    {
        SelectedJobExecutionId = executionId;
        await JobExecutionModal.LetAsync(x => x.ShowAsync());
    }

    private void OnJobSubmitted(Job job)
    {
        var remove = Jobs_?.FirstOrDefault(j => j.JobId == job.JobId);
        if (remove is not null)
        {
            job.Schedules = remove.Schedules;
            Jobs_?.Remove(remove);
        }
        else
        {
            job.Schedules = new List<Schedule>();
        }
        Jobs_?.Add(job);
        Jobs_?.Sort((j1, j2) => j1.JobName.CompareTo(j2.JobName));
    }

    private void OnCategorySubmitted(JobCategory category)
    {
        var remove = Categories?.FirstOrDefault(c => c.CategoryId == category.CategoryId);
        if (remove is not null)
        {
            Categories?.Remove(remove);
        }
        Categories?.Add(category);
        Categories?.Sort((c1, c2) => c1.CategoryName.CompareTo(c2.CategoryName));
    }

    private async Task DeleteCategoryAsync(JobCategory category)
    {
        try
        {
            using var context = DbFactory.CreateDbContext();
            context.JobCategories.Remove(category);
            await context.SaveChangesAsync();
            Categories?.Remove(category);
            foreach (var job in Jobs_?.Where(t => t.CategoryId == category.CategoryId) ?? Enumerable.Empty<Job>())
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

    private void ExpandAll()
    {
        foreach (var category in Categories ?? Enumerable.Empty<JobCategory>())
        {
            var state = CategoryExpandStatuses.GetOrCreateValue(category);
            state.IsExpanded = true;
        }
        NoCategoryExpanded.IsExpanded = true;
    }

    private void CollapseAll()
    {
        foreach (var category in Categories ?? Enumerable.Empty<JobCategory>())
        {
            var state = CategoryExpandStatuses.GetOrCreateValue(category);
            state.IsExpanded = false;
        }
        NoCategoryExpanded.IsExpanded = false;
    }

    private class ExpandStatus { public bool IsExpanded { get; set; } = true; }
}
