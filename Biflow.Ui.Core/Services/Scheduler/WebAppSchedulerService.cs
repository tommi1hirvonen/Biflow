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
        var status = await GetStatusAsync();
        if (!status.TryPickT0(out Success _, out var _))
        {
            return;
        }

        const string endpoint = "/schedules/removejob";
        var schedulerJob = new SchedulerJob(jobId);
        var json = JsonSerializer.Serialize(schedulerJob);
        var content = new StringContent(json, Encoding.UTF8, "application/json");
        var response = await _httpClient.PostAsync(endpoint, content);
        response.EnsureSuccessStatusCode();
    }

    public async Task AddScheduleAsync(Schedule schedule)
    {
        var status = await GetStatusAsync();
        if (!status.TryPickT0(out Success _, out var _))
        {
            return;
        }

        const string endpoint = "/schedules/add";
        var schedulerSchedule = SchedulerSchedule.From(schedule);
        var json = JsonSerializer.Serialize(schedulerSchedule);
        var content = new StringContent(json, Encoding.UTF8, "application/json");
        var response = await _httpClient.PostAsync(endpoint, content);
        response.EnsureSuccessStatusCode();
    }

    public async Task RemoveScheduleAsync(Schedule schedule)
    {
        var status = await GetStatusAsync();
        if (!status.TryPickT0(out Success _, out var _))
        {
            return;
        }

        const string endpoint = "/schedules/remove";
        var schedulerSchedule = SchedulerSchedule.From(schedule);
        var json = JsonSerializer.Serialize(schedulerSchedule);
        var content = new StringContent(json, Encoding.UTF8, "application/json");
        var response = await _httpClient.PostAsync(endpoint, content);
        response.EnsureSuccessStatusCode();
    }

    public async Task UpdateScheduleAsync(Schedule schedule)
    {
        var status = await GetStatusAsync();
        if (!status.TryPickT0(out Success _, out var _))
        {
            return;
        }

        const string endpoint = "/schedules/update";
        var schedulerSchedule = SchedulerSchedule.From(schedule);
        var json = JsonSerializer.Serialize(schedulerSchedule);
        var content = new StringContent(json, Encoding.UTF8, "application/json");
        var response = await _httpClient.PostAsync(endpoint, content);
        response.EnsureSuccessStatusCode();
    }

    public async Task<SchedulerStatusResponse> GetStatusAsync()
    {
        const string endpoint = "/schedules/status";
        var response = await _httpClient.GetAsync(endpoint);
        if (!response.IsSuccessStatusCode)
        {
            return response.StatusCode switch
            {
                System.Net.HttpStatusCode.Forbidden => new AuthorizationError(),
                System.Net.HttpStatusCode.InternalServerError => new SchedulerError(),
                _ => new UndefinedError()
            };
        }
        var jobs = await response.Content.ReadFromJsonAsync<IEnumerable<JobStatus>>();
        return new Success(jobs ?? []);
    }

    public async Task SynchronizeAsync()
    {
        const string endpoint = "/schedules/synchronize";
        var response = await _httpClient.GetAsync(endpoint);
        response.EnsureSuccessStatusCode();
    }

    public async Task ToggleScheduleEnabledAsync(Schedule schedule, bool enabled)
    {
        var status = await GetStatusAsync();
        if (!status.TryPickT0(out _, out _))
        {
            return;
        }

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
}
