using Microsoft.Extensions.Configuration;
using System.Web;

namespace Biflow.Ui.Core;

public class WebAppExecutorService(IConfiguration configuration, IHttpClientFactory httpClientFactory) : IExecutorService
{
    private readonly IConfiguration _configuration = configuration;
    private readonly HttpClient _httpClient = httpClientFactory.CreateClient("DefaultCredentials");

    private string Url => _configuration
        .GetSection("Executor")
        .GetSection("WebApp")
        .GetValue<string>("Url") ?? throw new ArgumentNullException(nameof(Url));

    public async Task StartExecutionAsync(Guid executionId)
    {
        var response = await _httpClient.GetAsync($"{Url}/execution/start/{executionId}");
        response.EnsureSuccessStatusCode();
    }

    public async Task StopExecutionAsync(Guid executionId, Guid stepId, string username)
    {
        var encodedUsername = HttpUtility.UrlEncode(username);
        var url = $"{Url}/execution/stop/{executionId}/{stepId}?username={encodedUsername}";
        var response = await _httpClient.GetAsync(url);
        response.EnsureSuccessStatusCode();
    }

    public async Task StopExecutionAsync(Guid executionId, string username)
    {
        var encodedUsername = HttpUtility.UrlEncode(username);
        var url = $"{Url}/execution/stop/{executionId}?username={encodedUsername}";
        var response = await _httpClient.GetAsync(url);
        response.EnsureSuccessStatusCode();
    }
}
