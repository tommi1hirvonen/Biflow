using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Biflow.Scheduler.Core.ServiceHealth;

internal class SchedulesReadCheck(ISchedulesManager schedulesManager) : IHealthCheck
{
    public Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        return Task.FromResult(schedulesManager.DatabaseReadException is { } ex
            ? HealthCheckResult.Unhealthy(
                description: "Error reading schedules from app database",
                exception: ex,
                data: new Dictionary<string, object> { { "LastChecked", DateTimeOffset.UtcNow } })
            : HealthCheckResult.Healthy(
                description: "Schedules successfully read from app database",
                data: new Dictionary<string, object> { { "LastChecked", DateTimeOffset.UtcNow } }));
    }
}