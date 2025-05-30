﻿using Biflow.Scheduler.Core;
using Microsoft.Extensions.Hosting;

namespace Biflow.Ui.Core;

public interface ISchedulerService
{
    public Task DeleteJobAsync(Guid jobId);

    public Task<IEnumerable<JobStatus>> GetStatusAsync();

    public Task AddScheduleAsync(Schedule schedule);

    public Task RemoveScheduleAsync(Schedule schedule);

    public Task UpdateScheduleAsync(Schedule schedule);

    public Task SynchronizeAsync();

    public Task ToggleScheduleEnabledAsync(Schedule schedule, bool enabled);
    
    public Task<HealthReportDto> GetHealthReportAsync(CancellationToken cancellationToken = default);
    
    public Task ClearTransientHealthErrorsAsync(CancellationToken cancellationToken = default);
}