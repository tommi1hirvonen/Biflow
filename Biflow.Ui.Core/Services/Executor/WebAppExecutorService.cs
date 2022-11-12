using Biflow.DataAccess.Models;
using Microsoft.Extensions.Configuration;
using System.Web;

namespace Biflow.Ui.Core;

public class WebAppExecutorService : IExecutorService
{
    private readonly IConfiguration _configuration;
    private readonly HttpClient _httpClient;

    private string Url => _configuration
        .GetSection("Executor")
        .GetSection("WebApp")
        .GetValue<string>("Url") ?? throw new ArgumentNullException(nameof(Url));

    public WebAppExecutorService(IConfiguration configuration, IHttpClientFactory httpClientFactory)
    {
        _configuration = configuration;
        _httpClient = httpClientFactory.CreateClient("DefaultCredentials");
    }

    public async Task StartExecutionAsync(Guid executionId)
    {
        var response = await _httpClient.GetAsync($"{Url}/execution/start/{executionId}");
        response.EnsureSuccessStatusCode();
    }

    public async Task StopExecutionAsync(StepExecutionAttempt attempt, string username)
    {
        var encodedUsername = HttpUtility.UrlEncode(username);
        var url = $"{Url}/execution/stop/{attempt.ExecutionId}/{attempt.StepId}?username={encodedUsername}";
        var response = await _httpClient.GetAsync(url);
        response.EnsureSuccessStatusCode();
    }

    public async Task StopExecutionAsync(Execution execution, string username)
    {
        var encodedUsername = HttpUtility.UrlEncode(username);
        var url = $"{Url}/execution/stop/{execution.ExecutionId}?username={encodedUsername}";
        var response = await _httpClient.GetAsync(url);
        response.EnsureSuccessStatusCode();
    }
}
