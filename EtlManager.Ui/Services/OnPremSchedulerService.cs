using Dapper;
using EtlManager.DataAccess.Models;
using EtlManager.Utilities;
using Microsoft.Data.SqlClient;
using System.IO.Pipes;
using System.ServiceProcess;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace EtlManager.Ui;

public class OnPremSchedulerService : ISchedulerService
{
    private readonly IConfiguration _configuration;

    private string ServiceName => _configuration
        .GetSection("Scheduler")
        .GetSection("OnPrem")
        .GetValue<string>("ServiceName");

    private string PipeName => _configuration
        .GetSection("Scheduler")
        .GetSection("OnPrem")
        .GetValue<string>("PipeName");

    public OnPremSchedulerService(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public Task<(bool Running, bool Error, string Status)> GetStatusAsync()
    {
        try
        {
#pragma warning disable CA1416 // Validate platform compatibility
            var serviceController = new ServiceController(ServiceName);
            var status = serviceController.Status switch
            {
                ServiceControllerStatus.Running => "Running",
                ServiceControllerStatus.Stopped => "Stopped",
                ServiceControllerStatus.Paused => "Paused",
                ServiceControllerStatus.StopPending => "Stopping",
                ServiceControllerStatus.StartPending => "Starting",
                ServiceControllerStatus.ContinuePending => "Continue pending",
                ServiceControllerStatus.PausePending => "Pause pending",
                _ => "Unknown"
            };
            return Task.FromResult((status == "Running", false, status));
#pragma warning restore CA1416 // Validate platform compatibility
        }
        catch (Exception)
        {
            return Task.FromResult((false, true, "Unknown"));
        }
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

        // Connect to the pipe server set up by the scheduler service.
        using var pipeClient = new NamedPipeClientStream(".", PipeName, PipeDirection.InOut); // "." => the pipe server is on the same computer
        await pipeClient.ConnectAsync(10000); // wait for 10 seconds
#pragma warning disable CA1416 // Validate platform compatibility
        pipeClient.ReadMode = PipeTransmissionMode.Message; // Each byte array is transferred as a single message
#pragma warning restore CA1416 // Validate platform compatibility

        // Send delete command.
        var command = new SchedulerCommand(SchedulerCommand.CommandType.Delete, job.JobId.ToString(), null, null);
        var json = JsonSerializer.Serialize(command, SchedulerCommandSerializerOptions());
        var bytes = Encoding.UTF8.GetBytes(json);
        pipeClient.Write(bytes, 0, bytes.Length);


        // Get response from scheduler service
        var responseBytes = CommonUtility.ReadMessage(pipeClient);
        var response = Encoding.UTF8.GetString(responseBytes);
        return response == "SUCCESS";
    }

    public async Task<bool> SendCommandAsync(SchedulerCommand.CommandType commandType, Schedule? schedule)
    {
        // If the scheduler service is not running, return true.
        // This way the changes can be committed to the database.
        (var running, var _, var _) = await GetStatusAsync();

        if (!running)
            return true;

        // Connect to the pipe server set up by the scheduler service.
        using var pipeClient = new NamedPipeClientStream(".", PipeName, PipeDirection.InOut); // "." => the pipe server is on the same computer
        await pipeClient.ConnectAsync(10000); // wait for 10 seconds
#pragma warning disable CA1416 // Validate platform compatibility
        pipeClient.ReadMode = PipeTransmissionMode.Message; // Each byte array is transferred as a single message
#pragma warning restore CA1416 // Validate platform compatibility

        var command = new SchedulerCommand(commandType, schedule?.JobId.ToString(), schedule?.ScheduleId.ToString(), schedule?.CronExpression);
        var json = JsonSerializer.Serialize(command, SchedulerCommandSerializerOptions());
        var bytes = Encoding.UTF8.GetBytes(json);
        pipeClient.Write(bytes, 0, bytes.Length);


        // Get response from scheduler service
        var responseBytes = CommonUtility.ReadMessage(pipeClient);
        var response = Encoding.UTF8.GetString(responseBytes);
        return response == "SUCCESS";
    }

    public async Task<bool> SynchronizeAsync()
    {
        // Connect to the pipe server set up by the scheduler service.
        using var pipeClient = new NamedPipeClientStream(".", PipeName, PipeDirection.InOut); // "." => the pipe server is on the same computer
        await pipeClient.ConnectAsync(10000); // wait for 10 seconds
#pragma warning disable CA1416 // Validate platform compatibility
        pipeClient.ReadMode = PipeTransmissionMode.Message; // Each byte array is transferred as a single message
#pragma warning restore CA1416 // Validate platform compatibility

        // Send synchronize command
        var command = new SchedulerCommand(SchedulerCommand.CommandType.Synchronize, null, null, null);
        var json = JsonSerializer.Serialize(command, SchedulerCommandSerializerOptions());
        var bytes = Encoding.UTF8.GetBytes(json);
        pipeClient.Write(bytes, 0, bytes.Length);

        // Get response from the scheduler service.
        var responseBytes = CommonUtility.ReadMessage(pipeClient);
        var response = Encoding.UTF8.GetString(responseBytes);
        return response == "SUCCESS";
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
