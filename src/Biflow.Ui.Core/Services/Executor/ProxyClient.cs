using System.Net.Http.Json;
using System.Text.Json;
using Biflow.ExecutorProxy.Core.FilesExplorer;

namespace Biflow.Ui.Core;

public class ProxyClient
{
    private readonly HttpClient _httpClient;
    
    private static readonly JsonSerializerOptions JsonSerializerOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };
    
    public ProxyClient(IHttpClientFactory httpClientFactory, Proxy proxy)
    {
        _httpClient = httpClientFactory.CreateClient();
        _httpClient.BaseAddress = new Uri(proxy.ProxyUrl);
        if (proxy.ApiKey is not null)
        {
            _httpClient.DefaultRequestHeaders.Add("x-api-key", proxy.ApiKey);
        }
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

    public async Task TestConnectionAsync(CancellationToken cancellationToken = default)
    {
        var response = await _httpClient.GetAsync("/health", cancellationToken);
        response.EnsureSuccessStatusCode();
    }
}