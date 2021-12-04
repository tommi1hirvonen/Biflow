using Dapper;
using EtlManager.DataAccess.Models;
using EtlManager.Scheduler.Core;
using EtlManager.Utilities;
using Microsoft.Data.SqlClient;

namespace EtlManager.Ui.Services;

public class SelfHostedSchedulerService : ISchedulerService
{
    private readonly ISchedulesManager _schedulesManager;
    private readonly IConfiguration _configuration;

    private bool DatabaseReadError { get; set; } = false;

    public SelfHostedSchedulerService(ISchedulesManager schedulesManager, IConfiguration configuration)
    {
        _schedulesManager = schedulesManager;
        _configuration = configuration;
    }

    public async Task<bool> DeleteJobAsync(Job job)
    {
        var command = new SchedulerCommand(SchedulerCommand.CommandType.Delete, job.JobId.ToString(), null, null);
        await _schedulesManager.RemoveScheduleAsync(command, CancellationToken.None);
        return true;
    }

    public Task<(bool Running, bool Error, string Status)> GetStatusAsync()
    {
        return Task.FromResult((true, DatabaseReadError, "Running"));
    }

    public async Task<bool> SendCommandAsync(SchedulerCommand.CommandType commandType, Schedule? schedule)
    {
        var command = new SchedulerCommand(commandType, schedule?.JobId.ToString(), schedule?.ScheduleId.ToString(), schedule?.CronExpression);
        switch (command.Type)
        {
            case SchedulerCommand.CommandType.Add:
                await _schedulesManager.AddScheduleAsync(command, CancellationToken.None);
                break;
            case SchedulerCommand.CommandType.Delete:
                await _schedulesManager.RemoveScheduleAsync(command, CancellationToken.None);
                break;
            case SchedulerCommand.CommandType.Pause:
                await _schedulesManager.PauseScheduleAsync(command, CancellationToken.None);
                break;
            case SchedulerCommand.CommandType.Resume:
                await _schedulesManager.ResumeScheduleAsync(command, CancellationToken.None);
                break;
        }
        return true;
    }

    public async Task<bool> SynchronizeAsync()
    {
        await _schedulesManager.ReadAllSchedules(CancellationToken.None);
        return true;
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
