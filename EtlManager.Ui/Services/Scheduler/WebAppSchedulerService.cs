using EtlManager.DataAccess.Models;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace EtlManager.Ui;

public class WebAppSchedulerService : ISchedulerService
{
    private readonly IConfiguration _configuration;
    private readonly HttpClient _httpClient;

    private string Url => _configuration
        .GetSection("Scheduler")
        .GetSection("WebApp")
        .GetValue<string>("Url");

    public WebAppSchedulerService(IConfiguration configuration, IHttpClientFactory httpClientFactory)
    {
        _configuration = configuration;
        _httpClient = httpClientFactory.CreateClient();
    }

    private JsonSerializerOptions Options { get; } = new()
    {
        ReferenceHandler = ReferenceHandler.IgnoreCycles
    };

    public async Task DeleteJobAsync(Job job)
    {
        (var running, var _) = await GetStatusAsync();
        if (!running) return;

        var endpoint = $"{Url}/jobs/remove";
        var json = JsonSerializer.Serialize(job, Options);
        var content = new StringContent(json, Encoding.UTF8, "application/json");
        var response = await _httpClient.PostAsync(endpoint, content);
        response.EnsureSuccessStatusCode();
    }

    public async Task AddScheduleAsync(Schedule schedule)
    {
        (var running, var _) = await GetStatusAsync();
        if (!running) return;

        var endpoint = $"{Url}/schedules/add";
        var json = JsonSerializer.Serialize(schedule, Options);
        var content = new StringContent(json, Encoding.UTF8, "application/json");
        var response = await _httpClient.PostAsync(endpoint, content);
        response.EnsureSuccessStatusCode();
    }

    public async Task RemoveScheduleAsync(Schedule schedule)
    {
        (var running, var _) = await GetStatusAsync();
        if (!running) return;

        var endpoint = $"{Url}/schedules/remove";
        var json = JsonSerializer.Serialize(schedule, Options);
        var content = new StringContent(json, Encoding.UTF8, "application/json");
        var response = await _httpClient.PostAsync(endpoint, content);
        response.EnsureSuccessStatusCode();
    }

    public async Task<(bool SchedulerDetected, bool SchedulerError)> GetStatusAsync()
    {
        try
        {
            var endpoint = $"{Url}/status";
            var response = await _httpClient.GetAsync(endpoint);
            response.EnsureSuccessStatusCode();
            var content = await response.Content.ReadAsStringAsync();
            if (content == "SUCCESS")
            {
                return (true, false);
            }
            else
            {
                return (true, true);
            }
        }
        catch (Exception)
        {
            return (false, true);
        }
    }

    public async Task SynchronizeAsync()
    {
        var endpoint = $"{Url}/schedules/synchronize";
        var response = await _httpClient.GetAsync(endpoint);
        response.EnsureSuccessStatusCode();
    }

    public async Task ToggleScheduleEnabledAsync(Schedule schedule, bool enabled)
    {
        (var running, var _) = await GetStatusAsync();
        if (!running) return;

        var json = JsonSerializer.Serialize(schedule, Options);
        var content = new StringContent(json, Encoding.UTF8, "application/json");
        var endpoint = enabled switch { true => $"{Url}/schedules/resume", false => $"{Url}/schedules/pause" };
        var response = await _httpClient.PostAsync(endpoint, content);
        response.EnsureSuccessStatusCode();
    }
    
}
