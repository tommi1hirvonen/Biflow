using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Net.Http.Json;
using System.Text.Json.Serialization;

namespace Biflow.DataAccess.Models;

[Table("QlikCloudClient")]
public class QlikCloudClient
{
    [Key]
    [JsonInclude]
    public Guid QlikCloudClientId { get; private set; }

    [Required]
    public required string QlikCloudClientName { get; set; }

    [Required]
    public required string EnvironmentUrl { get; set; }

    [Required]
    [JsonSensitive]
    public required string ApiToken { get; set; }

    [JsonIgnore]
    public ICollection<QlikStep> Steps { get; set; } = null!;

    public async Task TestConnectionAsync(CancellationToken cancellationToken = default)
    {
        var url = $"{EnvironmentUrl}/api/v1/spaces?limit=1";
        using var httpClient = CreateHttpClient();
        var response = await httpClient.GetAsync(url, cancellationToken);
        response.EnsureSuccessStatusCode();
    }

    public async Task<string> GetAppNameAsync(string appId, CancellationToken cancellationToken = default)
    {
        var url = $"{EnvironmentUrl}/api/v1/apps/{appId}";
        using var httpClient = CreateHttpClient();
        var response = await httpClient.GetFromJsonAsync<GetAppResponse>(url, cancellationToken);
        ArgumentNullException.ThrowIfNull(response);
        return response.Attributes.Name;
    }

    public async Task<IEnumerable<QlikSpace>> GetAppsAsync(CancellationToken cancellationToken = default)
    {
        var url = $"{EnvironmentUrl}/api/v1/items?limit=100&resourceType=app";
        using var httpClient = CreateHttpClient();
        var items = new List<ItemData>();
        do
        {
            var response = await httpClient.GetFromJsonAsync<GetItemsResponse>(url, cancellationToken);
            ArgumentNullException.ThrowIfNull(response);
            items.AddRange(response.Data);
            url = response.Links.Next?.Href;
        } while (url is not null);

        var spaces = await GetSpacesAsync(httpClient, cancellationToken);
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

    private async Task<IEnumerable<SpaceData>> GetSpacesAsync(HttpClient httpClient, CancellationToken cancellationToken)
    {
        var url = $"{EnvironmentUrl}/api/v1/spaces?limit=100";
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

    private HttpClient CreateHttpClient()
    {
        var client = new HttpClient();
        client.DefaultRequestHeaders.Authorization = new("Bearer", ApiToken);
        return client;
    }

    private record GetAppResponse(GetAppResponseAttributes Attributes);

    private record GetAppResponseAttributes(string Name);

    private record GetItemsResponse(ItemData[] Data, Links Links);

    private record ItemData(ItemResourceAttributes ResourceAttributes);

    private record ItemResourceAttributes(string Id, string Name, string SpaceId);

    private record GetSpacesResponse(SpaceData[] Data, Links Links);

    private record SpaceData(string Id, string Name);

    private record Links(Link? Next);

    private record Link(string Href);
}

public record QlikSpace(string Id, string Name, IEnumerable<QlikApp> Apps);

public record QlikApp(string Id, string Name);