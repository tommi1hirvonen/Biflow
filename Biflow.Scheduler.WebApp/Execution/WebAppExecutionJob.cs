using Biflow.DataAccess;
using Biflow.Executor.Core;
using Biflow.Scheduler.Core;
using Microsoft.EntityFrameworkCore;
using Polly;
using Polly.Retry;
using System.Net;

namespace Biflow.Scheduler.WebApp;

public class WebAppExecutionJob(
    IConfiguration configuration,
    ILogger<WebAppExecutionJob> logger,
    IDbContextFactory<SchedulerDbContext> dbContextFactory,
    IExecutionBuilderFactory<SchedulerDbContext> executionBuilderFactory,
    IHttpClientFactory httpClientFactory) : ExecutionJobBase(logger, dbContextFactory, executionBuilderFactory)
{
    private readonly IConfiguration _configuration = configuration;
    private readonly HttpClient _httpClient = httpClientFactory.CreateClient();
    private readonly ILogger<WebAppExecutionJob> _logger = logger;

    private const int PollingIntervalMs = 5000;

    private string Url => _configuration
        .GetSection("Executor")
        .GetSection("WebApp")
        .GetValue<string>("Url") ?? throw new ArgumentNullException(nameof(Url));

    private static readonly AsyncRetryPolicy<HttpResponseMessage> RetryPolicy = Policy
        // Executor status endpoint returns OK if the execution is running or NotFound if the execution is not running.
        // Exceptions and other status codes can be considered incorrect results => retry.
        .HandleResult<HttpResponseMessage>(response => response is not { StatusCode: HttpStatusCode.NotFound or HttpStatusCode.OK})
        .Or<Exception>()
        .WaitAndRetryAsync(3, retryCount => TimeSpan.FromMilliseconds(PollingIntervalMs));

    protected override async Task StartExecutorAsync(Guid executionId)
    {
        var response = await _httpClient.GetAsync($"{Url}/executions/start/{executionId}");
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
                response = await RetryPolicy.ExecuteAsync(() => _httpClient.GetAsync($"{Url}/executions/status/{executionId}"));
            } while (response.IsSuccessStatusCode);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error while monitoring status for execution {executionId}", executionId);
        }
    }
}
