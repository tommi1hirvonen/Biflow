using System.Net;
using Biflow.Scheduler.Core;
using Microsoft.Extensions.Configuration;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Hosting;

namespace Biflow.Ui.Core;

public class WebAppSchedulerService : ISchedulerService
{
    private readonly HttpClient _httpClient;
    
    private static readonly JsonSerializerOptions JsonSerializerOptions = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() }
    };

    public WebAppSchedulerService(IConfiguration configuration, IHttpClientFactory httpClientFactory)
    {
        _httpClient = httpClientFactory.CreateClient();

        var section = configuration
            .GetSection("Scheduler")
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

    public async Task DeleteJobAsync(Guid jobId)
    {
        const string endpoint = "/schedules/removejob";
        var schedulerJob = new SchedulerJob(jobId);
        var json = JsonSerializer.Serialize(schedulerJob);
        var content = new StringContent(json, Encoding.UTF8, "application/json");
        var response = await _httpClient.PostAsync(endpoint, content);
        response.EnsureSuccessStatusCode();
    }

    public async Task AddScheduleAsync(Schedule schedule)
    {
        const string endpoint = "/schedules/add";
        var schedulerSchedule = SchedulerSchedule.From(schedule);
        var json = JsonSerializer.Serialize(schedulerSchedule);
        var content = new StringContent(json, Encoding.UTF8, "application/json");
        var response = await _httpClient.PostAsync(endpoint, content);
        response.EnsureSuccessStatusCode();
    }

    public async Task RemoveScheduleAsync(Schedule schedule)
    {
        const string endpoint = "/schedules/remove";
        var schedulerSchedule = SchedulerSchedule.From(schedule);
        var json = JsonSerializer.Serialize(schedulerSchedule);
        var content = new StringContent(json, Encoding.UTF8, "application/json");
        var response = await _httpClient.PostAsync(endpoint, content);
        response.EnsureSuccessStatusCode();
    }

    public async Task UpdateScheduleAsync(Schedule schedule)
    {
        const string endpoint = "/schedules/update";
        var schedulerSchedule = SchedulerSchedule.From(schedule);
        var json = JsonSerializer.Serialize(schedulerSchedule);
        var content = new StringContent(json, Encoding.UTF8, "application/json");
        var response = await _httpClient.PostAsync(endpoint, content);
        response.EnsureSuccessStatusCode();
    }

    public async Task<IEnumerable<JobStatus>> GetStatusAsync()
    {
        const string endpoint = "/schedules/status";
        var response = await _httpClient.GetAsync(endpoint);
        response.EnsureSuccessStatusCode();
        var jobs = await response.Content.ReadFromJsonAsync<IEnumerable<JobStatus>>();
        return jobs ?? [];
    }

    public async Task SynchronizeAsync()
    {
        const string endpoint = "/schedules/synchronize";
        var response = await _httpClient.GetAsync(endpoint);
        response.EnsureSuccessStatusCode();
    }

    public async Task ToggleScheduleEnabledAsync(Schedule schedule, bool enabled)
    {
        var schedulerSchedule = SchedulerSchedule.From(schedule);
        var json = JsonSerializer.Serialize(schedulerSchedule);
        var content = new StringContent(json, Encoding.UTF8, "application/json");
        var endpoint = enabled switch
        {
            true => "/schedules/resume",
            false => "/schedules/pause"
        };
        var response = await _httpClient.PostAsync(endpoint, content);
        response.EnsureSuccessStatusCode();
    }

    public async Task<HealthReportDto> GetHealthReportAsync(CancellationToken cancellationToken = default)
    {
        const string endpoint = "/health";
        using var timeoutCts = new CancellationTokenSource(TimeSpan.FromSeconds(10));
        using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, timeoutCts.Token);
        var response = await _httpClient.GetAsync(endpoint, linkedCts.Token);
        if (response.StatusCode is not (HttpStatusCode.OK or HttpStatusCode.ServiceUnavailable))
        {
            response.EnsureSuccessStatusCode();
        }
        var healthReport = await response.Content.ReadFromJsonAsync<HealthReportDto>(JsonSerializerOptions,
            cancellationToken: linkedCts.Token);
        ArgumentNullException.ThrowIfNull(healthReport);
        return healthReport;
    }
    
    public async Task ClearTransientHealthErrorsAsync(CancellationToken cancellationToken = default)
    {
        const string endpoint = "/health/clear";
        var response = await _httpClient.PostAsync(endpoint, null, cancellationToken);
        response.EnsureSuccessStatusCode();
    }
}
