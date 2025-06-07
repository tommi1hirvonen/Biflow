using Biflow.DataAccess;
using Biflow.Scheduler.Core;
using Microsoft.EntityFrameworkCore;
using Polly;
using Polly.Retry;
using System.Net;
using Biflow.Core;
using JetBrains.Annotations;

namespace Biflow.Scheduler.WebApp.Execution;

[UsedImplicitly]
public class WebAppExecutionJob(
    ILogger<WebAppExecutionJob> logger,
    [FromKeyedServices(SchedulerServiceKeys.JobStartFailuresHealthService)]
    HealthService healthService,
    IDbContextFactory<SchedulerDbContext> dbContextFactory,
    IExecutionBuilderFactory<SchedulerDbContext> executionBuilderFactory,
    IHttpClientFactory httpClientFactory)
    : ExecutionJobBase(logger, healthService, dbContextFactory, executionBuilderFactory)
{
    private readonly HttpClient _httpClient = httpClientFactory.CreateClient("executor");

    private const int PollingIntervalMs = 5000;

    private static readonly AsyncRetryPolicy<HttpResponseMessage> RetryPolicy = Policy
        // Executor status endpoint returns OK if the execution is running or NotFound if the execution is not running.
        // Exceptions and other status codes can be considered incorrect results => retry.
        .HandleResult<HttpResponseMessage>(response => response is not { StatusCode: HttpStatusCode.NotFound or HttpStatusCode.OK})
        .Or<Exception>()
        .WaitAndRetryAsync(3, retryCount => TimeSpan.FromMilliseconds(PollingIntervalMs));

    protected override async Task StartExecutorAsync(Guid executionId)
    {
        var response = await _httpClient.GetAsync($"/executions/start/{executionId}");
        response.EnsureSuccessStatusCode();
    }

    protected override async Task WaitForExecutionToFinish(Guid executionId)
    {
        try
        {
            HttpResponseMessage response;
            do
            {
                await Task.Delay(PollingIntervalMs);
                response = await RetryPolicy.ExecuteAsync(() => _httpClient.GetAsync($"/executions/status/{executionId}"));
            } while (response.IsSuccessStatusCode);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error while monitoring status for execution {executionId}", executionId);
        }
    }
}
