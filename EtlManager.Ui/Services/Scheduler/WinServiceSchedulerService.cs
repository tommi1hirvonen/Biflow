using EtlManager.DataAccess.Models;
using EtlManager.Utilities;
using System.IO.Pipes;
using System.ServiceProcess;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace EtlManager.Ui;

public class WinServiceSchedulerService : ISchedulerService
{
    private readonly IConfiguration _configuration;

    private string ServiceName => _configuration
        .GetSection("Scheduler")
        .GetSection("WinService")
        .GetValue<string>("ServiceName");

    private string PipePrefix => _configuration
        .GetSection("Scheduler")
        .GetSection("WinService")
        .GetValue<string>("PipeName");

    public WinServiceSchedulerService(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    private JsonSerializerOptions Options { get; } = new()
    {
        ReferenceHandler = ReferenceHandler.IgnoreCycles
    };

    public async Task<(bool SchedulerDetected, bool SchedulerError)> GetStatusAsync()
    {
        var serviceController = new ServiceController(ServiceName);
        if (serviceController.Status != ServiceControllerStatus.Running)
        {
            return (false, false);
        }

        var pipeName = $"{PipePrefix}_STATUS";
        var response = await SendNamedPipeMessageAsync(pipeName, "STATUS");
        var schedulerError = response != "SUCCESS";

        return (true, schedulerError);
    }

    public async Task AddScheduleAsync(Schedule schedule)
    {
        (var running, var _) = await GetStatusAsync();
        if (!running) return;

        var pipeName = $"{PipePrefix}_SCHEDULES_ADD";
        var message = JsonSerializer.Serialize(schedule, Options);
        var response = await SendNamedPipeMessageAsync(pipeName, message);
        if (response != "SUCCESS") throw new Exception("Scheduler encountered an error when adding schedule.");
    }

    public async Task RemoveScheduleAsync(Schedule schedule)
    {
        (var running, var _) = await GetStatusAsync();
        if (!running) return;

        var pipeName = $"{PipePrefix}_SCHEDULES_REMOVE";
        var message = JsonSerializer.Serialize(schedule, Options);
        var response = await SendNamedPipeMessageAsync(pipeName, message);
        if (response != "SUCCESS") throw new Exception("Scheduler encountered an error when removing schedule.");
    }

    public async Task DeleteJobAsync(Job job)
    {
        (var running, var _) = await GetStatusAsync();
        if (!running) return;

        var pipeName = $"{PipePrefix}_JOBS_REMOVE";
        var message = JsonSerializer.Serialize(job, Options);
        var response = await SendNamedPipeMessageAsync(pipeName, message);
        if (response != "SUCCESS") throw new Exception("Scheduler encountered an error when deleting job and its schedules.");
    }

    public async Task SynchronizeAsync()
    {
        var pipeName = $"{PipePrefix}_SCHEDULES_SYNCHRONIZE";
        var response = await SendNamedPipeMessageAsync(pipeName, "SYNCHRONIZE");
        if (response != "SUCCESS") throw new Exception("Scheduler encountered an error when synchronizing.");
    }

    public async Task ToggleScheduleEnabledAsync(Schedule schedule, bool enabled)
    {
        (var running, var _) = await GetStatusAsync();
        if (!running) return;

        var pipeName = enabled ? $"{PipePrefix}_SCHEDULES_RESUME" : $"{PipePrefix}_SCHEDULES_PAUSE";
        var message = JsonSerializer.Serialize(schedule, Options);
        var response = await SendNamedPipeMessageAsync(pipeName, message);
        if (response != "SUCCESS") throw new Exception("Scheduler encountered an error when toggling schedule state.");
    }

    private static async Task<string> SendNamedPipeMessageAsync(string pipeName, string message)
    {
        // Connect to the pipe server set up by the scheduler service.
        using var pipeClient = new NamedPipeClientStream(".", pipeName, PipeDirection.InOut); // "." => the pipe server is on the same computer
        await pipeClient.ConnectAsync(10000); // wait for 10 seconds
        pipeClient.ReadMode = PipeTransmissionMode.Message; // Each byte array is transferred as a single message

        var bytes = Encoding.UTF8.GetBytes(message);
        pipeClient.Write(bytes, 0, bytes.Length);

        // Get response from scheduler service
        var responseBytes = CommonUtility.ReadMessage(pipeClient);
        var response = Encoding.UTF8.GetString(responseBytes);
        return response;
    }
    
}
