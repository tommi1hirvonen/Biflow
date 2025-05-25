using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Biflow.Executor.Core.ServiceHealth;

public class NotificationHealthCheck(
    [FromKeyedServices(ExecutorServiceKeys.NotificationHealthService)] HealthService service) : IHealthCheck
{
    public Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        return Task.FromResult(
            service.Errors.Count > 0 
                ? HealthCheckResult.Degraded(
                    "Notification errors detected.",
                    data: new Dictionary<string, object> { { "executionIds", service.Errors } })
                : HealthCheckResult.Healthy());
    }
}