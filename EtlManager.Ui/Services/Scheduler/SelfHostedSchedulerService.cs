using EtlManager.DataAccess.Models;
using EtlManager.Scheduler.Core;

namespace EtlManager.Ui.Services;

public class SelfHostedSchedulerService : ISchedulerService
{
    private readonly ISchedulesManager _schedulesManager;

    private bool DatabaseReadError { get; set; } = false;

    public SelfHostedSchedulerService(ISchedulesManager schedulesManager)
    {
        _schedulesManager = schedulesManager;
    }

    public async Task AddScheduleAsync(Schedule schedule) => await _schedulesManager.AddScheduleAsync(schedule, CancellationToken.None);

    public async Task RemoveScheduleAsync(Schedule schedule) => await _schedulesManager.RemoveScheduleAsync(schedule, CancellationToken.None);

    public async Task DeleteJobAsync(Job job) => await _schedulesManager.RemoveJobAsync(job, CancellationToken.None);

    public Task<(bool SchedulerDetected, bool SchedulerError)> GetStatusAsync() => Task.FromResult((true, DatabaseReadError));

    public async Task SynchronizeAsync()
    {
        try
        {
            await _schedulesManager.ReadAllSchedules(CancellationToken.None);
            DatabaseReadError = false;
        }
        catch (Exception)
        {
            DatabaseReadError = true;
            throw;
        }
    }

    public async Task ToggleScheduleEnabledAsync(Schedule schedule, bool enabled)
    {
        if (enabled)
        {
            await _schedulesManager.ResumeScheduleAsync(schedule, CancellationToken.None);
        }
        else
        {
            await _schedulesManager.PauseScheduleAsync(schedule, CancellationToken.None);
        }
    }

}
