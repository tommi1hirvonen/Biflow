using EtlManager.DataAccess.Models;
using EtlManager.Scheduler.Core;
using EtlManager.Utilities;
using System.IO.Pipes;
using System.Text;
using System.Text.Json;

namespace EtlManager.Scheduler;

public class Worker : BackgroundService
{
    private readonly IConfiguration _config;
    private readonly ILogger<Worker> _logger;
    private readonly ISchedulesManager _schedulesManager;

    private bool DatabaseReadError { get; set; } = false;

    private byte[] FailureBytes { get; } = Encoding.UTF8.GetBytes("FAILURE");
    private byte[] SuccessBytes { get; } = Encoding.UTF8.GetBytes("SUCCESS");

    private string PipePrefix => _config.GetValue<string>("PipeName");

    public Worker(IConfiguration config, ILogger<Worker> logger, ISchedulesManager schedulesManager)
    {
        _config = config;
        _logger = logger;
        _schedulesManager = schedulesManager;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
            await _schedulesManager.ReadAllSchedules(stoppingToken);
        }
        catch (Exception ex)
        {
            DatabaseReadError = true;
            _logger.LogError(ex, "Error reading schedules from database");
        }

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var tasks = new[]
                {
                    ReadNamedPipeAddSchedule(stoppingToken),
                    ReadNamedPipeRemoveSchedule(stoppingToken),
                    ReadNamedPipeRemoveJob(stoppingToken),
                    ReadNamedPipePauseSchedule(stoppingToken),
                    ReadNamedPipeResumeSchedule(stoppingToken),
                    ReadNamedPipeSynchronize(stoppingToken),
                    ReadNamedPipeStatus(stoppingToken)
                };
                await Task.WhenAll(tasks);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error reading named pipe");
                await Task.Delay(10000, stoppingToken);
            }
        }
    }

    private async Task ReadNamedPipeAddSchedule(CancellationToken cancellationToken)
    {
        var pipeName = $"{PipePrefix}_SCHEDULES_ADD";
        await ReadNamedPipe(pipeName, async pipeServer =>
        {
            try
            {
                var messageBytes = CommonUtility.ReadMessage(pipeServer);
                var json = Encoding.UTF8.GetString(messageBytes);
                var schedule = JsonSerializer.Deserialize<Schedule>(json);
                ArgumentNullException.ThrowIfNull(schedule);
                await _schedulesManager.AddScheduleAsync(schedule, cancellationToken);
                pipeServer.Write(SuccessBytes, 0, SuccessBytes.Length);
            }
            catch (Exception)
            {
                pipeServer.Write(FailureBytes, 0, FailureBytes.Length);
            }
        }, cancellationToken);
    }

    private async Task ReadNamedPipeRemoveSchedule(CancellationToken cancellationToken)
    {
        var pipeName = $"{PipePrefix}_SCHEDULES_REMOVE";
        await ReadNamedPipe(pipeName, async pipeServer =>
        {
            try
            {
                var messageBytes = CommonUtility.ReadMessage(pipeServer);
                var json = Encoding.UTF8.GetString(messageBytes);
                var schedule = JsonSerializer.Deserialize<Schedule>(json);
                ArgumentNullException.ThrowIfNull(schedule);
                await _schedulesManager.RemoveScheduleAsync(schedule, cancellationToken);
                pipeServer.Write(SuccessBytes, 0, SuccessBytes.Length);
            }
            catch (Exception)
            {
                pipeServer.Write(FailureBytes, 0, FailureBytes.Length);
            }
        }, cancellationToken);
    }

    private async Task ReadNamedPipeRemoveJob(CancellationToken cancellationToken)
    {
        var pipeName = $"{PipePrefix}_JOBS_REMOVE";
        await ReadNamedPipe(pipeName, async pipeServer =>
        {
            try
            {
                var messageBytes = CommonUtility.ReadMessage(pipeServer);
                var json = Encoding.UTF8.GetString(messageBytes);
                var job = JsonSerializer.Deserialize<Job>(json);
                ArgumentNullException.ThrowIfNull(job);
                await _schedulesManager.RemoveJobAsync(job, cancellationToken);
                pipeServer.Write(SuccessBytes, 0, SuccessBytes.Length);
            }
            catch (Exception)
            {
                pipeServer.Write(FailureBytes, 0, FailureBytes.Length);
            }
        }, cancellationToken);
    }

    private async Task ReadNamedPipePauseSchedule(CancellationToken cancellationToken)
    {
        var pipeName = $"{PipePrefix}_SCHEDULES_PAUSE";
        await ReadNamedPipe(pipeName, async pipeServer =>
        {
            try
            {
                var messageBytes = CommonUtility.ReadMessage(pipeServer);
                var json = Encoding.UTF8.GetString(messageBytes);
                var schedule = JsonSerializer.Deserialize<Schedule>(json);
                ArgumentNullException.ThrowIfNull(schedule);
                await _schedulesManager.PauseScheduleAsync(schedule, cancellationToken);
                pipeServer.Write(SuccessBytes, 0, SuccessBytes.Length);
            }
            catch (Exception)
            {
                pipeServer.Write(FailureBytes, 0, FailureBytes.Length);
            }
        }, cancellationToken);
    }

    private async Task ReadNamedPipeResumeSchedule(CancellationToken cancellationToken)
    {
        var pipeName = $"{PipePrefix}_SCHEDULES_RESUME";
        await ReadNamedPipe(pipeName, async pipeServer =>
        {
            try
            {
                var messageBytes = CommonUtility.ReadMessage(pipeServer);
                var json = Encoding.UTF8.GetString(messageBytes);
                var schedule = JsonSerializer.Deserialize<Schedule>(json);
                ArgumentNullException.ThrowIfNull(schedule);
                await _schedulesManager.ResumeScheduleAsync(schedule, cancellationToken);
                pipeServer.Write(SuccessBytes, 0, SuccessBytes.Length);
            }
            catch (Exception)
            {
                pipeServer.Write(FailureBytes, 0, FailureBytes.Length);
            }
        }, cancellationToken);
    }

    private async Task ReadNamedPipeSynchronize(CancellationToken cancellationToken)
    {
        var pipeName = $"{PipePrefix}_SCHEDULES_SYNCHRONIZE";
        await ReadNamedPipe(pipeName, async pipeServer =>
        {
            try
            {
                var _ = CommonUtility.ReadMessage(pipeServer);
                await _schedulesManager.ReadAllSchedules(cancellationToken);
                pipeServer.Write(SuccessBytes, 0, SuccessBytes.Length);
                DatabaseReadError = false;
            }
            catch (Exception)
            {
                pipeServer.Write(FailureBytes, 0, FailureBytes.Length);
                DatabaseReadError = true;
            }
        }, cancellationToken);
    }

    private async Task ReadNamedPipeStatus(CancellationToken cancellationToken)
    {
        var pipeName = $"{PipePrefix}_STATUS";
        await ReadNamedPipe(pipeName, pipeServer =>
        {
            try
            {
                var _ = CommonUtility.ReadMessage(pipeServer);
                if (DatabaseReadError)
                {
                    pipeServer.Write(FailureBytes, 0, FailureBytes.Length);
                }
                else
                {
                    pipeServer.Write(SuccessBytes, 0, SuccessBytes.Length);
                }
            }
            catch (Exception)
            {
                pipeServer.Write(FailureBytes, 0, FailureBytes.Length);
            }
            return Task.CompletedTask;
        }, cancellationToken);
    }

    private static async Task ReadNamedPipe(string pipeName, Func<NamedPipeServerStream, Task> whenConnected, CancellationToken cancellationToken)
    {
        while (true)
        {
            using var pipeServer = new NamedPipeServerStream(
                pipeName,
                PipeDirection.InOut,
                NamedPipeServerStream.MaxAllowedServerInstances,
                PipeTransmissionMode.Message); // Each byte array is transferred as a single message

            await pipeServer.WaitForConnectionAsync(cancellationToken);
            await whenConnected(pipeServer);
        }
    }

}
