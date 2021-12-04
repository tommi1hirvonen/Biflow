using EtlManager.DataAccess.Models;
using EtlManager.Utilities;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace EtlManager.Ui;

public class WebAppExecutorService : IExecutorService
{
    private readonly IConfiguration _configuration;
    private readonly HttpClient _httpClient;

    private string Url => _configuration
        .GetSection("Executor")
        .GetSection("WebApp")
        .GetValue<string>("Url");

    public WebAppExecutorService(IConfiguration configuration, IHttpClientFactory httpClientFactory)
    {
        _configuration = configuration;
        _httpClient = httpClientFactory.CreateClient();
    }

    private static JsonSerializerOptions CommandSerializerOptions() =>
        new() { Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) } };

    public async Task StartExecutionAsync(Guid executionId, bool notify, SubscriptionType? notifyMe, bool notifyMeOvertime)
    {
        var command = new StartCommand
        {
            ExecutionId = executionId,
            NotifyMe = notifyMe,
            Notify = notify,
            NotifyMeOvertime = notifyMeOvertime
        };
        var json = JsonSerializer.Serialize(command, CommandSerializerOptions());
        var content = new StringContent(json, Encoding.UTF8, "application/json");
        var response = await _httpClient.PostAsync($"{Url}/execution/start", content);
        response.EnsureSuccessStatusCode();
    }

    public async Task StopExecutionAsync(StepExecutionAttempt attempt, string username)
    {
        var command = new StopCommand
        {
            ExecutionId = attempt.ExecutionId,
            Username = username,
            StepId = attempt.StepId,
        };
        var json = JsonSerializer.Serialize(command, CommandSerializerOptions());
        var content = new StringContent(json, Encoding.UTF8, "application/json");
        var response = await _httpClient.PostAsync($"{Url}/execution/stop", content);
        response.EnsureSuccessStatusCode();
    }

    public async Task StopExecutionAsync(Execution execution, string username)
    {
        var command = new StopCommand
        {
            ExecutionId = execution.ExecutionId,
            Username = username
        };
        var json = JsonSerializer.Serialize(command, CommandSerializerOptions());
        var content = new StringContent(json, Encoding.UTF8, "application/json");
        var response = await _httpClient.PostAsync($"{Url}/execution/stop", content);
        response.EnsureSuccessStatusCode();
    }
}
