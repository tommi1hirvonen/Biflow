using Biflow.DataAccess;
using Biflow.Scheduler.Core;
using Microsoft.EntityFrameworkCore;

namespace Biflow.Scheduler.WebApp;

public class WebAppExecutionJob : ExecutionJobBase
{
    private readonly IConfiguration _configuration;
    private readonly HttpClient _httpClient;
    private readonly ILogger<WebAppExecutionJob> _logger;

    private const int PollingIntervalMs = 5000;

    public WebAppExecutionJob(
        IConfiguration configuration,
        ILogger<WebAppExecutionJob> logger,
        IDbContextFactory<BiflowContext> dbContextFactory,
        IExecutionBuilderFactory executionBuilderFactory,
        IHttpClientFactory httpClientFactory)
        : base(logger, dbContextFactory, executionBuilderFactory)
    {
        _configuration = configuration;
        _httpClient = httpClientFactory.CreateClient();
        _logger = logger;
    }

    private string Url => _configuration
        .GetSection("Executor")
        .GetSection("WebApp")
        .GetValue<string>("Url") ?? throw new ArgumentNullException(nameof(Url));

    protected override async Task StartExecutorAsync(Guid executionId)
    {
        var response = await _httpClient.GetAsync($"{Url}/execution/start/{executionId}");
        response.EnsureSuccessStatusCode();
    }

    protected override async Task WaitForExecutionToFinish(Guid executionId)
    {
        try
        {
            bool running;
            do
            {
                await Task.Delay(PollingIntervalMs);
                var response = await _httpClient.GetAsync($"{Url}/execution/status/{executionId}");
                response.EnsureSuccessStatusCode();
                var content = await response.Content.ReadAsStringAsync();
                running = content == "RUNNING";
            } while (running);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error while monitoring status for execution {executionId}", executionId);
        }
    }
}
