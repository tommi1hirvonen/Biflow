using Microsoft.IdentityModel.Clients.ActiveDirectory;
using System.ComponentModel.DataAnnotations;
using System.Text.Json;

namespace EtlManager.DataAccess.Models;

public class FunctionApp
{
    [Required]
    [Display(Name = "Function app id")]
    public Guid FunctionAppId { get; set; }

    [Required]
    [Display(Name = "Function app name")]
    public string? FunctionAppName { get; set; }

    [Display(Name = "Function app key")]
    public string? FunctionAppKey
    {
        get => _functionAppKey;
        set => _functionAppKey = string.IsNullOrEmpty(value) ? null : value;
    }

    private string? _functionAppKey;

    [Required]
    [Display(Name = "Subscription id")]
    [MaxLength(36)]
    [MinLength(36)]
    public string? SubscriptionId { get; set; }

    [Required]
    [Display(Name = "Resource group name")]
    public string? ResourceGroupName { get; set; }

    [Required]
    [Display(Name = "Resource name")]
    public string? ResourceName { get; set; }

    [Required]
    [Display(Name = "App registration")]
    public Guid? AppRegistrationId { get; set; }

    public AppRegistration AppRegistration { get; set; } = null!;

    public IList<FunctionStep> Steps { get; set; } = null!;

    private const string AuthenticationUrl = "https://login.microsoftonline.com/";
    private const string ResourceUrl = "https://management.azure.com/";

    public async Task<List<(string FunctionName, string FunctionUrl)>> GetFunctionsAsync(HttpClient client, ITokenService tokenService)
    {
        var accessToken = await tokenService.GetTokenAsync(AppRegistration, ResourceUrl);
        var functionListUrl = $"https://management.azure.com/subscriptions/{SubscriptionId}/resourceGroups/{ResourceGroupName}/providers/Microsoft.Web/sites/{ResourceName}/functions?api-version=2015-08-01";
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
            string type = "";
            if (bindings.MoveNext())
            {
                type = bindings.Current.GetProperty("type").GetString() ?? "";
            }
            return (name, type, url);
        })
        .Where(f => f.url is not null && f.type == "httpTrigger")
        .Select(f => (f.name, f.url))
        .ToList();

        return functions;
    }

    public async Task<List<(string Type, string Key)>> GetHostKeysAsync(HttpClient client, ITokenService tokenService)
    {
        var accessToken = await tokenService.GetTokenAsync(AppRegistration, ResourceUrl);
        var hostKeysUrl = $"https://management.azure.com/subscriptions/{SubscriptionId}/resourceGroups/{ResourceGroupName}/providers/Microsoft.Web/sites/{ResourceName}/host/default/listkeys?api-version=2019-08-01";
        var message = new HttpRequestMessage(HttpMethod.Post, hostKeysUrl);
        message.Headers.Add("authorization", $"Bearer {accessToken}");
        var response = await client.SendAsync(message);
        var content = await response.Content.ReadAsStringAsync();
        var options = new JsonSerializerOptions() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };
        var json = JsonSerializer.Deserialize<HostKeys>(content, options) ?? throw new InvalidOperationException("JSON object was null");
        var list =
            new List<(string, string)> { ("masterKey", json.MasterKey) }
            .Concat(json.FunctionKeys.Select(f => (f.Key, f.Value)))
            .ToList();
        return list;
    }

    private record HostKeys(string MasterKey, Dictionary<string, string> FunctionKeys);

    public async Task TestConnection(HttpClient client)
    {
        var context = new AuthenticationContext(AuthenticationUrl + AppRegistration.TenantId);
        var clientCredential = new ClientCredential(AppRegistration.ClientId, AppRegistration.ClientSecret);
        var result = await context.AcquireTokenAsync(ResourceUrl, clientCredential);

        var functionListUrl = $"https://management.azure.com/subscriptions/{SubscriptionId}/resourceGroups/{ResourceGroupName}/providers/Microsoft.Web/sites/{ResourceName}/functions?api-version=2015-08-01";
        var message = new HttpRequestMessage(HttpMethod.Get, functionListUrl);
        message.Headers.Add("authorization", $"Bearer {result.AccessToken}");
        var response = await client.SendAsync(message);
        response.EnsureSuccessStatusCode();
    }
}
