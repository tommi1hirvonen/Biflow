using Biflow.DataAccess.Models;
using Biflow.Scheduler.Core;
using Microsoft.Extensions.Configuration;
using System.Text;
using System.Text.Json;

namespace Biflow.Ui.Core;

public class WebAppSchedulerService : ISchedulerService
{
    private readonly IConfiguration _configuration;
    private readonly HttpClient _httpClient;

    private string Url => _configuration
        .GetSection("Scheduler")
        .GetSection("WebApp")
        .GetValue<string>("Url") ?? throw new ArgumentNullException(nameof(Url));

    public WebAppSchedulerService(IConfiguration configuration, IHttpClientFactory httpClientFactory)
    {
        _configuration = configuration;
        _httpClient = httpClientFactory.CreateClient("DefaultCredentials");
    }

    public async Task DeleteJobAsync(Job job)
    {
        var status = await GetStatusAsync();
        if (!status.TryPickT0(out Success _, out var _))
        {
            return;
        }

        var endpoint = $"{Url}/jobs/remove";
        var schedulerJob = new SchedulerJob(job.JobId);
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
        ArgumentNullException.ThrowIfNull(schedule.CronExpression);
        var schedulerSchedule = new SchedulerSchedule(schedule.ScheduleId, schedule.JobId, schedule.CronExpression, schedule.DisallowConcurrentExecution);
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
        ArgumentNullException.ThrowIfNull(schedule.CronExpression);
        var schedulerSchedule = new SchedulerSchedule(schedule.ScheduleId, schedule.JobId, schedule.CronExpression, schedule.DisallowConcurrentExecution);
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
        ArgumentNullException.ThrowIfNull(schedule.CronExpression);
        var schedulerSchedule = new SchedulerSchedule(schedule.ScheduleId, schedule.JobId, schedule.CronExpression, schedule.DisallowConcurrentExecution);
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
            return new Success();
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

        ArgumentNullException.ThrowIfNull(schedule.CronExpression);
        ArgumentNullException.ThrowIfNull(schedule.JobId);
        var schedulerSchedule = new SchedulerSchedule(schedule.ScheduleId, schedule.JobId, schedule.CronExpression, schedule.DisallowConcurrentExecution);
        var json = JsonSerializer.Serialize(schedulerSchedule);
        var content = new StringContent(json, Encoding.UTF8, "application/json");
        var endpoint = enabled switch { true => $"{Url}/schedules/resume", false => $"{Url}/schedules/pause" };
        var response = await _httpClient.PostAsync(endpoint, content);
        response.EnsureSuccessStatusCode();
    }

}
