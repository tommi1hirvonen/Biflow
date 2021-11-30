using EtlManager.DataAccess;
using EtlManager.Scheduler.Core;
using EtlManager.Utilities;
using Microsoft.EntityFrameworkCore;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace EtlManager.Scheduler.WebApp;

public class ExecutionJob : ExecutionJobBase
{
    private readonly IConfiguration _configuration;
    private readonly HttpClient _httpClient;
    private readonly ILogger<ExecutionJob> _logger;

    private const int PollingIntervalMs = 5000;

    public ExecutionJob(
        IConfiguration configuration,
        ILogger<ExecutionJob> logger,
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

    private static JsonSerializerOptions CommandSerializerOptions() =>
        new() { Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) } };

    private string Url => _configuration
        .GetSection("Executor")
        .GetValue<string>("Url");

    protected override async Task StartExecutorAsync(Guid executionId)
    {
        var command = new StartCommand
        {
            ExecutionId = executionId,
            NotifyMe = null,
            Notify = true,
            NotifyMeOvertime = false
        };
        var json = JsonSerializer.Serialize(command, CommandSerializerOptions());
        var content = new StringContent(json, Encoding.UTF8, "application/json");
        var response = await _httpClient.PostAsync($"{Url}/execution/start", content);
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
