using System.Net.Http.Json;
using System.Text.Json;
using JetBrains.Annotations;

namespace Biflow.Core.Entities;

public class QlikCloudClient
{
    private readonly HttpClient _httpClient;

    private static readonly JsonSerializerOptions DeserializerOptions = new() { PropertyNameCaseInsensitive = true };

    public QlikCloudClient(QlikCloudEnvironment qlikClient, IHttpClientFactory httpClientFactory)
    {
        _httpClient = httpClientFactory.CreateClient();
        _httpClient.DefaultRequestHeaders.Authorization = new("Bearer", qlikClient.ApiToken);
        var url = qlikClient.EnvironmentUrl.EndsWith('/')
            ? qlikClient.EnvironmentUrl
            : $"{qlikClient.EnvironmentUrl}/";
        _httpClient.BaseAddress = new Uri(url);
    }

    public async Task TestConnectionAsync(CancellationToken cancellationToken = default)
    {
        const string url = "api/v1/spaces?limit=1";
        var response = await _httpClient.GetAsync(url, cancellationToken);
        response.EnsureSuccessStatusCode();
    }

    public async Task<QlikAppReload> ReloadAppAsync(string appId, CancellationToken cancellationToken = default)
    {
        const string postReloadUrl = "api/v1/reloads";
        var message = new
        {
            appId,
            partial = false
        };
        var response = await _httpClient.PostAsJsonAsync(postReloadUrl, message, cancellationToken);
        response.EnsureSuccessStatusCode();
        var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);
        ArgumentNullException.ThrowIfNull(responseBody);
        var reload = JsonSerializer.Deserialize<QlikAppReload>(responseBody, DeserializerOptions)
            ?? throw new ApplicationException("Reload response was null");
        return reload;
    }

    public async Task<QlikAutomationRun> RunAutomationAsync(string automationId, CancellationToken cancellationToken = default)
    {
        var postRunUrl = $"api/v1/automations/{automationId}/runs";
        var message = new
        {
            context = "api"
        };
        var response = await _httpClient.PostAsJsonAsync(postRunUrl, message, cancellationToken);
        response.EnsureSuccessStatusCode();
        var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);
        ArgumentNullException.ThrowIfNull(responseBody);
        var run = JsonSerializer.Deserialize<QlikAutomationRun>(responseBody, DeserializerOptions)
            ?? throw new ApplicationException("Run response was null");
        return run;
    }

    public async Task<QlikAppReload> GetReloadAsync(string reloadId, CancellationToken cancellationToken = default)
    {
        var getReloadUrl = $"api/v1/reloads/{reloadId}";
        var reload = await _httpClient.GetFromJsonAsync<QlikAppReload>(getReloadUrl, cancellationToken)
            ?? throw new ApplicationException("Reload response was null");
        return reload;
    }

    public async Task<QlikAutomationRun> GetRunAsync(string automationId, string runId, CancellationToken cancellationToken = default)
    {
        var getRunUrl = $"api/v1/automations/{automationId}/runs/{runId}";
        var run = await _httpClient.GetFromJsonAsync<QlikAutomationRun>(getRunUrl, cancellationToken)
            ?? throw new ApplicationException("Run response was null");
        return run;
    }

    public async Task CancelReloadAsync(string reloadId, CancellationToken cancellationToken = default)
    {
        var cancelUrl = $"api/v1/reloads/{reloadId}/actions/cancel";
        var response = await _httpClient.PostAsync(cancelUrl, null, cancellationToken);
        response.EnsureSuccessStatusCode();
    }

    public async Task CancelRunAsync(string automationId, string runId, CancellationToken cancellationToken = default)
    {
        var cancelUrl = $"api/v1/automations/{automationId}/runs/{runId}/actions/stop";
        var response = await _httpClient.PostAsync(cancelUrl , null, cancellationToken);
        response.EnsureSuccessStatusCode();
    }

    public async Task<QlikApp?> GetAppAsync(string appId, CancellationToken cancellationToken = default)
    {
        var url = $"api/v1/apps/{appId}";
        var response = await _httpClient.GetFromJsonAsync<GetAppResponse>(url, cancellationToken);
        return response is not null
            ? new(response.Attributes.Id, response.Attributes.Name)
            : null;
    }

    public async Task<QlikAutomation?> GetAutomationAsync(string automationId, CancellationToken cancellationToken = default)
    {
        var url = $"api/v1/automations/{automationId}";
        var response = await _httpClient.GetFromJsonAsync<QlikAutomation>(url, cancellationToken);
        return response is not null
            ? new(response.Id, response.Name)
            : null;
    }

    public async Task<IEnumerable<QlikSpace>> GetAppsAsync(CancellationToken cancellationToken = default)
    {
        var url = "api/v1/items?limit=100&resourceType=app";
        var items = new List<ItemData>();
        do
        {
            var response = await _httpClient.GetFromJsonAsync<GetItemsResponse>(url, cancellationToken);
            ArgumentNullException.ThrowIfNull(response);
            items.AddRange(response.Data);
            url = response.Links.Next?.Href;
        } while (url is not null);

        var spaces = await GetSpacesAsync(_httpClient, cancellationToken);
        var result = spaces
            .Append(new("", "No space")) // Soome apps may not have a space, and their space id is an empty string. Add empty space to list of spaces.
            .Select(s =>
            {
                var apps = items
                    .Where(i => i.ResourceAttributes.SpaceId == s.Id)
                    .Select(a => new QlikApp(a.ResourceAttributes.Id, a.ResourceAttributes.Name))
                    .OrderBy(a => a.Name)
                    .ToArray();
                return new QlikSpace(s.Id, s.Name, apps);
            })
            .Where(s => s.Apps.Any())
            .OrderBy(s => s.Id == "")
            .ThenBy(s => s.Name)
            .ToArray();
        return result;
    }

    public async Task<IEnumerable<QlikAutomation>> GetAutomationsAsync(CancellationToken cancellationToken = default)
    {
        var url = "api/v1/automations?limit=200";
        var items = new List<QlikAutomation>();
        do
        {
            var response = await _httpClient.GetFromJsonAsync<GetAutomationsResponse>(url, cancellationToken);
            ArgumentNullException.ThrowIfNull(response);
            items.AddRange(response.Data);
            url = response.Links.Next?.Href;
        } while (url is not null);
        return items;
    }

    private static async Task<IEnumerable<SpaceData>> GetSpacesAsync(HttpClient httpClient, CancellationToken cancellationToken)
    {
        var url = "api/v1/spaces?limit=100";
        var spaces = new List<SpaceData>();
        do
        {
            var response = await httpClient.GetFromJsonAsync<GetSpacesResponse>(url, cancellationToken);
            ArgumentNullException.ThrowIfNull(response);
            spaces.AddRange(response.Data);
            url = response.Links.Next?.Href;
        } while (url is not null);
        return spaces;
    }
    
    private record GetAppResponse(GetAppResponseAttributes Attributes);

    [UsedImplicitly]
    private record GetAppResponseAttributes(string Id, string Name);

    private record GetItemsResponse(ItemData[] Data, Links Links);

    [UsedImplicitly]
    private record ItemData(ItemResourceAttributes ResourceAttributes);

    [UsedImplicitly]
    private record ItemResourceAttributes(string Id, string Name, string SpaceId);

    private record GetSpacesResponse(SpaceData[] Data, Links Links);

    private record SpaceData(string Id, string Name);

    private record GetAutomationsResponse(QlikAutomation[] Data, Links Links);

    [UsedImplicitly]
    private record Links(Link? Next);

    [UsedImplicitly]
    private record Link(string Href);
}
