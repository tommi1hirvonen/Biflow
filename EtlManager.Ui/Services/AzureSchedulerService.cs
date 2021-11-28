using Dapper;
using EtlManager.DataAccess.Models;
using EtlManager.Utilities;
using Microsoft.Data.SqlClient;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace EtlManager.Ui;

public class AzureSchedulerService : ISchedulerService
{
    private readonly IConfiguration _configuration;
    private readonly HttpClient _httpClient;

    private string Url => _configuration
        .GetSection("Scheduler")
        .GetSection("Azure")
        .GetValue<string>("Url");

    private string Endpoint => $"{Url}/scheduler";

    public AzureSchedulerService(IConfiguration configuration, IHttpClientFactory httpClientFactory)
    {
        _configuration = configuration;
        _httpClient = httpClientFactory.CreateClient();
    }

    private static JsonSerializerOptions SchedulerCommandSerializerOptions() =>
        new() { Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) } };

    public async Task<bool> DeleteJobAsync(Job job)
    {
        // If the scheduler service is not running, return true.
        // This way the changes can be committed to the database.
        (var running, var _, var _) = await GetStatusAsync();

        if (!running)
            return true;

        var command = new SchedulerCommand(SchedulerCommand.CommandType.Delete, job.JobId.ToString(), null, null);
        var json = JsonSerializer.Serialize(command, SchedulerCommandSerializerOptions());
        var content = new StringContent(json, Encoding.UTF8, "application/json");
        var response = await _httpClient.PostAsync(Endpoint, content);
        response.EnsureSuccessStatusCode();
        var responseContent = await response.Content.ReadAsStringAsync();
        return responseContent == "SUCCESS";
    }

    public async Task<(bool Running, bool Error, string Status)> GetStatusAsync()
    {
        try
        {
            var command = new SchedulerCommand(SchedulerCommand.CommandType.Status, null, null, null);
            var json = JsonSerializer.Serialize(command, SchedulerCommandSerializerOptions());
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            var response = await _httpClient.PostAsync(Endpoint, content);
            if (response.IsSuccessStatusCode)
            {
                return (true, false, "Running");
            }

            return (false, true, "Not running");
        }
        catch (Exception)
        {
            return (false, true, "Unknown");
        }
    }

    public async Task<bool> SendCommandAsync(SchedulerCommand.CommandType commandType, Schedule? schedule)
    {
        // If the scheduler service is not running, return true.
        // This way the changes can be committed to the database.
        (var running, var _, var _) = await GetStatusAsync();

        if (!running)
            return true;

        var command = new SchedulerCommand(commandType, schedule?.JobId.ToString(), schedule?.ScheduleId.ToString(), schedule?.CronExpression);
        var json = JsonSerializer.Serialize(command, SchedulerCommandSerializerOptions());
        var content = new StringContent(json, Encoding.UTF8, "application/json");
        var response = await _httpClient.PostAsync(Endpoint, content);
        response.EnsureSuccessStatusCode();
        var responseContent = await response.Content.ReadAsStringAsync();
        return responseContent == "SUCCESS";
    }

    public async Task<bool> SynchronizeAsync()
    {
        var command = new SchedulerCommand(SchedulerCommand.CommandType.Synchronize, null, null, null);
        var json = JsonSerializer.Serialize(command, SchedulerCommandSerializerOptions());
        var content = new StringContent(json, Encoding.UTF8, "application/json");
        var response = await _httpClient.PostAsync(Endpoint, content);
        response.EnsureSuccessStatusCode();
        var responseContent = await response.Content.ReadAsStringAsync();
        return responseContent == "SUCCESS";
    }

    public async Task<bool> ToggleScheduleEnabledAsync(Schedule schedule, bool enabled)
    {
        using var sqlConnection = new SqlConnection(_configuration.GetConnectionString("EtlManagerContext"));
        await sqlConnection.OpenAsync();
        using var transaction = sqlConnection.BeginTransaction();
        await sqlConnection.ExecuteAsync(
            @"UPDATE [etlmanager].[Schedule]
                SET [IsEnabled] = @Value
                WHERE [ScheduleId] = @ScheduleId", new { schedule.ScheduleId, Value = enabled }, transaction);
        var commandType = enabled ? SchedulerCommand.CommandType.Resume : SchedulerCommand.CommandType.Pause;
        bool success = await SendCommandAsync(commandType, schedule);
        if (success)
        {
            transaction.Commit();
            return true;
        }
        else
        {
            transaction.Rollback();
            return false;
        }
    }
}
