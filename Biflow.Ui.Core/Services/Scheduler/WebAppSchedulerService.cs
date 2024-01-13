using Biflow.Scheduler.Core;
using Microsoft.Extensions.Configuration;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;

namespace Biflow.Ui.Core;

public class WebAppSchedulerService(IConfiguration configuration, IHttpClientFactory httpClientFactory) : ISchedulerService
{
    private readonly IConfiguration _configuration = configuration;
    private readonly HttpClient _httpClient = httpClientFactory.CreateClient("DefaultCredentials");

    private string Url => _configuration
        .GetSection("Scheduler")
        .GetSection("WebApp")
        .GetValue<string>("Url") ?? throw new ArgumentNullException(nameof(Url));

    public async Task DeleteJobAsync(Guid jobId)
    {
        var status = await GetStatusAsync();
        if (!status.TryPickT0(out Success _, out var _))
        {
            return;
        }

        var endpoint = $"{Url}/jobs/remove";
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

        var endpoint = $"{Url}/schedules/add";
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

        var endpoint = $"{Url}/schedules/remove";
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

        var endpoint = $"{Url}/schedules/update";
        var schedulerSchedule = SchedulerSchedule.From(schedule);
        var json = JsonSerializer.Serialize(schedulerSchedule);
        var content = new StringContent(json, Encoding.UTF8, "application/json");
        var response = await _httpClient.PostAsync(endpoint, content);
        response.EnsureSuccessStatusCode();
    }

    public async Task<SchedulerStatusResponse> GetStatusAsync()
    {
        var endpoint = $"{Url}/status";
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
        var endpoint = $"{Url}/schedules/synchronize";
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
        var endpoint = enabled switch { true => $"{Url}/schedules/resume", false => $"{Url}/schedules/pause" };
        var response = await _httpClient.PostAsync(endpoint, content);
        response.EnsureSuccessStatusCode();
    }

}
