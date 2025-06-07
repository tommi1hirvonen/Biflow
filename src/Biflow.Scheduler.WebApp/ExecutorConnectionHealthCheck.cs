using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Biflow.Scheduler.WebApp;

public class ExecutorConnectionHealthCheck(IHttpClientFactory httpClientFactory) : IHealthCheck
{
    private readonly HttpClient _httpClient = httpClientFactory.CreateClient("executor");
    
    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            await _httpClient.GetAsync("/alive", cancellationToken);
            return HealthCheckResult.Healthy("Executor service connection test successful");
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Unhealthy("Executor service connection test failed", ex);
        }
    }
}