using EtlManager.Scheduler.Core;
using EtlManager.Utilities;
using System.IO.Pipes;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace EtlManager.Scheduler;

public class Worker : BackgroundService
{
    private readonly IConfiguration _config;
    private readonly ILogger<Worker> _logger;
    private readonly SchedulesManager<ServiceExecutionJob> _schedulesManager;

    private bool DatabaseReadError { get; set; } = false;

    private byte[] FailureBytes { get; } = Encoding.UTF8.GetBytes("FAILURE");
    private byte[] SuccessBytes { get; } = Encoding.UTF8.GetBytes("SUCCESS");

    private string PipeName => _config.GetValue<string>("PipeName");

    public Worker(IConfiguration config, ILogger<Worker> logger, SchedulesManager<ServiceExecutionJob> schedulesManager)
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
            DatabaseReadError = false;
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
                await ReadNamedPipeAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error reading named pipe");
                await Task.Delay(10000, stoppingToken);
            }
        }
    }

    private async Task ReadNamedPipeAsync(CancellationToken cancellationToken)
    {
        while (true)
        {
            _logger.LogInformation("Started waiting for named pipe connection for incoming commands");

            using var pipeServer = new NamedPipeServerStream(
                PipeName,
                PipeDirection.InOut,
                NamedPipeServerStream.MaxAllowedServerInstances,
                PipeTransmissionMode.Message); // Each byte array is transferred as a single message

            await pipeServer.WaitForConnectionAsync(cancellationToken);
            try
            {
                var messageBytes = CommonUtility.ReadMessage(pipeServer);
                var json = Encoding.UTF8.GetString(messageBytes);

                _logger.LogInformation($"Processing scheduler command: {json}");

                var options = new JsonSerializerOptions { Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) } };
                var command = JsonSerializer.Deserialize<SchedulerCommand>(json, options) ?? throw new ArgumentNullException("command", "Scheduler command was null");

                switch (command.Type)
                {
                    case SchedulerCommand.CommandType.Add:
                        await _schedulesManager.AddScheduleAsync(command, cancellationToken);
                        break;
                    case SchedulerCommand.CommandType.Delete:
                        await _schedulesManager.RemoveScheduleAsync(command, cancellationToken);
                        break;
                    case SchedulerCommand.CommandType.Pause:
                        await _schedulesManager.PauseScheduleAsync(command, cancellationToken);
                        break;
                    case SchedulerCommand.CommandType.Resume:
                        await _schedulesManager.ResumeScheduleAsync(command, cancellationToken);
                        break;
                    case SchedulerCommand.CommandType.Synchronize:
                        await SynchronizeSchedulerAsync(cancellationToken);
                        break;
                    case SchedulerCommand.CommandType.Status:
                        if (DatabaseReadError)
                        {
                            pipeServer.Write(FailureBytes, 0, FailureBytes.Length);
                            continue;
                        }
                        break;
                    default:
                        pipeServer.Write(FailureBytes, 0, FailureBytes.Length);
                        throw new ArgumentException($"Invalid command type {command.Type}");
                }

                pipeServer.Write(SuccessBytes, 0, SuccessBytes.Length);
            }
            catch (Exception ex)
            {
                pipeServer.Write(FailureBytes, 0, FailureBytes.Length);
                _logger.LogError(ex, "Error reading or executing named pipe command");
            }
        }
    }

    private async Task SynchronizeSchedulerAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Synchronizing scheduler with schedules from database");
        try
        {
            await _schedulesManager.ReadAllSchedules(cancellationToken);
            DatabaseReadError = false;
        }
        catch (Exception)
        {
            DatabaseReadError = true;
            throw;
        }
    }

}
