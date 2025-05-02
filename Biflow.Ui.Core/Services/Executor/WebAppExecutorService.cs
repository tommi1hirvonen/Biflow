using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using System.Web;
using Biflow.ExecutorProxy.Core.FilesExplorer;

namespace Biflow.Ui.Core;

public class WebAppExecutorService : IExecutorService
{
    private readonly HttpClient _httpClient;
    
    private static readonly JsonSerializerOptions JsonSerializerOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public WebAppExecutorService(IConfiguration configuration, IHttpClientFactory httpClientFactory)
    {
        _httpClient = httpClientFactory.CreateClient();

        var section = configuration
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

    public async Task ClearTokenCacheAsync(Guid azureCredentialId, CancellationToken cancellationToken = default)
    {
        var url = $"/tokencache/clear/{azureCredentialId}";
        var response = await _httpClient.GetAsync(url, cancellationToken);
        response.EnsureSuccessStatusCode();
    }

    public async Task<IReadOnlyList<DirectoryItem>> GetDirectoryItemsAsync(string? path,
        CancellationToken cancellationToken = default)
    {
        var request = new FileExplorerSearchRequest(path);
        var response = await _httpClient.PostAsJsonAsync("/fileexplorer/search", request, cancellationToken);
        response.EnsureSuccessStatusCode();
        await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        var result = await JsonSerializer.DeserializeAsync<FileExplorerSearchResponse>(
            stream, JsonSerializerOptions, cancellationToken);
        return result?.Items ?? [];
    }
}
