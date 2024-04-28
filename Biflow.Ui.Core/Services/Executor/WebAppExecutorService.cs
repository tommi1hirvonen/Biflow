using Microsoft.Extensions.Configuration;
using System.Web;

namespace Biflow.Ui.Core;

public class WebAppExecutorService : IExecutorService
{
    private readonly IConfiguration _configuration;
    private readonly HttpClient _httpClient;

    public WebAppExecutorService(IConfiguration configuration, IHttpClientFactory httpClientFactory)
    {
        _configuration = configuration;
        _httpClient = httpClientFactory.CreateClient();

        var section = _configuration
            .GetSection("Executor")
            .GetSection("WebApp");

        var apiKey = section.GetValue<string>("ApiKey");
        if (apiKey is not null)
        {
            _httpClient.DefaultRequestHeaders.Add("x-api-key", apiKey);
        }

        var baseUrl = section.GetValue<string>("Url");
        ArgumentNullException.ThrowIfNull(baseUrl);
        _httpClient.BaseAddress = new Uri(baseUrl);
    }

    public async Task StartExecutionAsync(Guid executionId, CancellationToken cancellationToken = default)
    {
        var response = await _httpClient.GetAsync($"/executions/start/{executionId}", cancellationToken);
        response.EnsureSuccessStatusCode();
    }

    public async Task StopExecutionAsync(Guid executionId, Guid stepId, string username)
    {
        var encodedUsername = HttpUtility.UrlEncode(username);
        var url = $"/executions/stop/{executionId}/{stepId}?username={encodedUsername}";
        var response = await _httpClient.GetAsync(url);
        if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            var message = await response.Content.ReadAsStringAsync();
            throw new Exception(message);
        }
        response.EnsureSuccessStatusCode();
    }

    public async Task StopExecutionAsync(Guid executionId, string username)
    {
        var encodedUsername = HttpUtility.UrlEncode(username);
        var url = $"/executions/stop/{executionId}?username={encodedUsername}";
        var response = await _httpClient.GetAsync(url);
        if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            var message = await response.Content.ReadAsStringAsync();
            throw new Exception(message);
        }
        response.EnsureSuccessStatusCode();
    }
}
