using Biflow.DataAccess;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Quartz;

namespace Biflow.Scheduler.Core;

public static class Extensions
{
    public static void AddSchedulerServices<TExecutionJob>(this IServiceCollection services)
        where TExecutionJob : ExecutionJobBase
    {
        services.AddQuartz(options => options.UseDefaultThreadPool(maxConcurrency: 100));
        services.AddQuartzHostedService(options => options.WaitForJobsToComplete = true);
        services.AddDbContextFactory<SchedulerDbContext>();
        services.AddExecutionBuilderFactory<SchedulerDbContext>();
        services.AddSingleton<ISchedulesManager, SchedulesManager<TExecutionJob>>();
        services.AddHostedService(s => s.GetRequiredService<ISchedulesManager>());
        services.AddHealthChecks().AddCheck<SchedulesReadCheck>("read_schedules");
    }

    private class SchedulesReadCheck(ISchedulesManager schedulesManager) : IHealthCheck
    {
        public Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(schedulesManager.DatabaseReadException is { } ex
                ? HealthCheckResult.Degraded("Error reading schedules from app database", ex)
                : HealthCheckResult.Healthy());
        }
    }
}
