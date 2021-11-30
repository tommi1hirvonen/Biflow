using EtlManager.Executor.Core;
using EtlManager.Executor.Core.Common;
using EtlManager.Utilities;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace EtlManager.Executor.WebApp;

internal class ExecutorLauncher : IExecutorLauncher
{
    private readonly IConfiguration _configuration;
    private readonly HttpClient _httpClient;
    private readonly IExecutionConfiguration _executionConfiguration;

    public ExecutorLauncher(IConfiguration configuration, IExecutionConfiguration executionConfiguration, IHttpClientFactory httpClientFactory)
    {
        _configuration = configuration;
        _httpClient = httpClientFactory.CreateClient();
        _executionConfiguration = executionConfiguration;
    }

    private string Url => _configuration
        .GetSection("Executor")
        .GetValue<string>("Url");

    private static JsonSerializerOptions CommandSerializerOptions() =>
        new() { Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) } };
    

    public async Task StartExecutorAsync(Guid executionId, bool notify)
    {
        var command = new StartCommand
        {
            ExecutionId = executionId,
            NotifyMe = null,
            Notify = notify,
            NotifyMeOvertime = false
        };
        var json = JsonSerializer.Serialize(command, CommandSerializerOptions());
        var content = new StringContent(json, Encoding.UTF8, "application/json");
        var response = await _httpClient.PostAsync($"{Url}/execution/start", content);
        response.EnsureSuccessStatusCode();
    }

    public async Task WaitForExitAsync(Guid executionId, CancellationToken cancellationToken)
    {
        bool running;
        do
        {
            await Task.Delay(_executionConfiguration.PollingIntervalMs, cancellationToken);
            var response = await _httpClient.GetAsync($"{Url}/execution/status/{executionId}", cancellationToken);
            response.EnsureSuccessStatusCode();
            var content = await response.Content.ReadAsStringAsync(cancellationToken);
            running = content == "RUNNING";
        } while (running);
    }

    public async Task CancelAsync(Guid executionId, string username)
    {
        var command = new StopCommand
        {
            ExecutionId = executionId,
            Username = username
        };
        var json = JsonSerializer.Serialize(command, CommandSerializerOptions());
        var content = new StringContent(json, Encoding.UTF8, "application/json");
        var response = await _httpClient.PostAsync($"{Url}/execution/stop", content);
        response.EnsureSuccessStatusCode();
    }
}
