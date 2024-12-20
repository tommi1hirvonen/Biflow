using Azure.Core;
using Biflow.Core.Interfaces;
using System.Text.Json;

namespace Biflow.Core.Entities;

public class FunctionAppClient(FunctionApp app, ITokenService tokenService, IHttpClientFactory httpClientFactory)
{
    private readonly IHttpClientFactory _httpClientFactory = httpClientFactory;
    private readonly ITokenService _tokenService = tokenService;
    private readonly string _subscriptionId = app.SubscriptionId;
    private readonly string _resourceGroupName = app.ResourceGroupName;
    private readonly string _resourceName = app.ResourceName;
    private readonly AzureCredential _azureCredential = app.AzureCredential;

    private const string ResourceUrl = "https://management.azure.com//.default";

    private static readonly JsonSerializerOptions SerializerOptions = new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

    public async Task<List<(string FunctionName, string FunctionUrl)>> GetFunctionsAsync()
    {
        using var client = _httpClientFactory.CreateClient();
        var (accessToken, _) = await _tokenService.GetTokenAsync(_azureCredential, [ResourceUrl]);
        var functionListUrl = $"https://management.azure.com/subscriptions/{_subscriptionId}/resourceGroups/{_resourceGroupName}/providers/Microsoft.Web/sites/{_resourceName}/functions?api-version=2015-08-01";
        var message = new HttpRequestMessage(HttpMethod.Get, functionListUrl);
        message.Headers.Add("authorization", $"Bearer {accessToken}");
        var response = await client.SendAsync(message);
        var content = await response.Content.ReadAsStringAsync();

        var json = JsonSerializer.Deserialize<JsonElement>(content);
        var value = json.GetProperty("value");
        var functionArray = value.EnumerateArray();
        var functions = functionArray.Select(func =>
        {
            var properties = func.GetProperty("properties");
            var name = properties.GetProperty("name").GetString() ?? "";
            var url = properties.GetProperty("invoke_url_template").GetString() ?? "";
            var config = properties.GetProperty("config");
            var bindings = config.GetProperty("bindings").EnumerateArray();
            var type = "";
            if (bindings.MoveNext())
            {
                type = bindings.Current.GetProperty("type").GetString() ?? "";
            }
            return (name, type, url);
        })
        .Where(f => f.type == "httpTrigger")
        .Select(f => (f.name, f.url))
        .ToList();

        return functions;
    }

    public async Task<List<(string Type, string Key)>> GetHostKeysAsync()
    {
        using var client = _httpClientFactory.CreateClient();
        var (accessToken, _) = await _tokenService.GetTokenAsync(_azureCredential, [ResourceUrl]);
        var hostKeysUrl = $"https://management.azure.com/subscriptions/{_subscriptionId}/resourceGroups/{_resourceGroupName}/providers/Microsoft.Web/sites/{_resourceName}/host/default/listkeys?api-version=2019-08-01";
        var message = new HttpRequestMessage(HttpMethod.Post, hostKeysUrl);
        message.Headers.Add("authorization", $"Bearer {accessToken}");
        var response = await client.SendAsync(message);
        var content = await response.Content.ReadAsStringAsync();
        if (!response.IsSuccessStatusCode)
        {
            throw new ApplicationException(content);
        }
        var json = JsonSerializer.Deserialize<HostKeys>(content, SerializerOptions) ?? throw new InvalidOperationException("JSON object was null");
        var list =
            new List<(string, string)> { ("masterKey", json.MasterKey) }
            .Concat(json.FunctionKeys.Select(f => (f.Key, f.Value)))
            .ToList();
        return list;
    }

    private record HostKeys(string MasterKey, Dictionary<string, string> FunctionKeys);

    public async Task TestConnection()
    {
        using var client = _httpClientFactory.CreateClient();
        var credential = _azureCredential.GetTokenCredential();
        var context = new TokenRequestContext([ResourceUrl]);
        var token = await credential.GetTokenAsync(context, CancellationToken.None);

        var functionListUrl = $"https://management.azure.com/subscriptions/{_subscriptionId}/resourceGroups/{_resourceGroupName}/providers/Microsoft.Web/sites/{_resourceName}/functions?api-version=2015-08-01";
        var message = new HttpRequestMessage(HttpMethod.Get, functionListUrl);
        message.Headers.Add("authorization", $"Bearer {token.Token}");
        var response = await client.SendAsync(message);
        response.EnsureSuccessStatusCode();
    }
}
