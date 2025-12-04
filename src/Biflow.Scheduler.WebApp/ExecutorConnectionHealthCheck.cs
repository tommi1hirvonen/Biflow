using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Biflow.Scheduler.WebApp;

public class ExecutorConnectionHealthCheck : IHealthCheck
{
    private readonly HttpClient _httpClient;

    public ExecutorConnectionHealthCheck(IHttpClientFactory httpClientFactory)
    {
        _httpClient = httpClientFactory.CreateClient("executor");
        // Use a lower timeout than the default.
        // This way the UI health check request doesn't time out before the scheduler health check timeouts.
        _httpClient.Timeout = TimeSpan.FromSeconds(15);
    }

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