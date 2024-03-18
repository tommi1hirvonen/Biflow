using Biflow.Scheduler.Core;
using Microsoft.Extensions.Configuration;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;

namespace Biflow.Ui.Core;

public class WebAppSchedulerService : ISchedulerService
{
    private readonly IConfiguration _configuration;
    private readonly HttpClient _httpClient;

    public WebAppSchedulerService(IConfiguration configuration, IHttpClientFactory httpClientFactory)
    {
        _configuration = configuration;
        _httpClient = httpClientFactory.CreateClient();

        var section = _configuration
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

        var endpoint = $"/schedules/removejob";
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

        var endpoint = $"/schedules/add";
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

        var endpoint = $"/schedules/remove";
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

        var endpoint = $"/schedules/update";
        var schedulerSchedule = SchedulerSchedule.From(schedule);
        var json = JsonSerializer.Serialize(schedulerSchedule);
        var content = new StringContent(json, Encoding.UTF8, "application/json");
        var response = await _httpClient.PostAsync(endpoint, content);
        response.EnsureSuccessStatusCode();
    }

    public async Task<SchedulerStatusResponse> GetStatusAsync()
    {
        var endpoint = $"/schedules/status";
        var response = await _httpClient.GetAsync(endpoint);
        if (response.IsSuccessStatusCode)
        {
            var jobs = await response.Content.ReadFromJsonAsync<IEnumerable<JobStatus>>();
            return new Success(jobs ?? []);
        }
        else if (response.StatusCode == System.Net.HttpStatusCode.Forbidden)
        {
            return new AuthorizationError();
        }
        else if (response.StatusCode == System.Net.HttpStatusCode.InternalServerError)
        {
            return new SchedulerError();
        }
        return new UndefinedError();
    }

    public async Task SynchronizeAsync()
    {
        var endpoint = $"/schedules/synchronize";
        var response = await _httpClient.GetAsync(endpoint);
        response.EnsureSuccessStatusCode();
    }

    public async Task ToggleScheduleEnabledAsync(Schedule schedule, bool enabled)
    {
        var status = await GetStatusAsync();
        if (!status.TryPickT0(out Success _, out var _))
        {
            return;
        }

        var schedulerSchedule = SchedulerSchedule.From(schedule);
        var json = JsonSerializer.Serialize(schedulerSchedule);
        var content = new StringContent(json, Encoding.UTF8, "application/json");
        var endpoint = enabled switch
        {
            true => $"/schedules/resume",
            false => $"/schedules/pause"
        };
        var response = await _httpClient.PostAsync(endpoint, content);
        response.EnsureSuccessStatusCode();
    }

}
