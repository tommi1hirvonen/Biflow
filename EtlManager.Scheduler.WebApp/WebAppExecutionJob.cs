using EtlManager.DataAccess;
using EtlManager.Scheduler.Core;
using Microsoft.EntityFrameworkCore;

namespace EtlManager.Scheduler.WebApp;

public class WebAppExecutionJob : ExecutionJobBase
{
    private readonly IConfiguration _configuration;
    private readonly HttpClient _httpClient;
    private readonly ILogger<WebAppExecutionJob> _logger;

    private const int PollingIntervalMs = 5000;

    public WebAppExecutionJob(
        IConfiguration configuration,
        ILogger<WebAppExecutionJob> logger,
        IDbContextFactory<EtlManagerContext> dbContextFactory,
        IHttpClientFactory httpClientFactory)
        : base(logger, dbContextFactory)
    {
        _configuration = configuration;
        _httpClient = httpClientFactory.CreateClient();
        _logger = logger;
    }

    protected override string EtlManagerConnectionString => _configuration.GetConnectionString("EtlManagerContext")
                ?? throw new ArgumentNullException("EtlManagerConnectionString", "Connection string cannot be null");

    private string Url => _configuration
        .GetSection("Executor")
        .GetSection("WebApp")
        .GetValue<string>("Url");

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
