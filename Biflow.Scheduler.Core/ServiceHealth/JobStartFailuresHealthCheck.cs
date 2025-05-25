using Biflow.Core;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Biflow.Scheduler.Core.ServiceHealth;

public class JobStartFailuresHealthCheck(
    [FromKeyedServices(SchedulerServiceKeys.JobStartFailuresHealthService)] HealthService service) : IHealthCheck
{
    public Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        return Task.FromResult(
            service.Errors.Count > 0 
                ? HealthCheckResult.Degraded(
                    "Job start failures detected.",
                    data: new Dictionary<string, object> { { "jobIds", service.Errors } })
                : HealthCheckResult.Healthy());
    }
}