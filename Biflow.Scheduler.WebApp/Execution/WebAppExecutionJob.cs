using Biflow.DataAccess;
using Biflow.Executor.Core;
using Biflow.Scheduler.Core;
using Microsoft.EntityFrameworkCore;
using Polly;
using Polly.Retry;
using System.Net;
using Biflow.Core;
using JetBrains.Annotations;

namespace Biflow.Scheduler.WebApp;

[UsedImplicitly]
public class WebAppExecutionJob : ExecutionJobBase
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<WebAppExecutionJob> _logger;

    private const int PollingIntervalMs = 5000;

    public WebAppExecutionJob(
        IConfiguration configuration,
        ILogger<WebAppExecutionJob> logger,
        [FromKeyedServices(SchedulerServiceKeys.JobStartFailuresHealthService)]
        HealthService healthService,
        IDbContextFactory<SchedulerDbContext> dbContextFactory,
        IExecutionBuilderFactory<SchedulerDbContext> executionBuilderFactory,
        IHttpClientFactory httpClientFactory) : base(logger, healthService, dbContextFactory, executionBuilderFactory)
    {
        _logger = logger;
        _httpClient = httpClientFactory.CreateClient();
        
        var apiKey = configuration
            .GetSection("Executor")
            .GetSection("WebApp")
            .GetValue<string>("ApiKey");
        if (apiKey is not null)
        {
            _httpClient.DefaultRequestHeaders.Add("x-api-key", apiKey);
        }

        var baseUrl = configuration
            .GetSection("Executor")
            .GetSection("WebApp")
            .GetValue<string>("Url");
        ArgumentNullException.ThrowIfNull(baseUrl);
        _httpClient.BaseAddress = new Uri(baseUrl);
    }

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
            _logger.LogError(ex, "Error while monitoring status for execution {executionId}", executionId);
        }
    }
}
