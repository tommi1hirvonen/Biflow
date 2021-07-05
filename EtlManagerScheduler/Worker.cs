using EtlManagerUtils;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Quartz;
using Quartz.Impl.Matchers;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.IO;
using System.IO.Pipes;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;

namespace EtlManagerScheduler
{
    public class Worker : BackgroundService
    {
        private readonly ILogger<Worker> _logger;
        private readonly IConfiguration _configuration;
        private readonly IScheduler _scheduler;

        private bool DatabaseReadError { get; set; } = false;

        private byte[] FailureBytes { get; } = Encoding.UTF8.GetBytes("FAILURE");
        private byte[] SuccessBytes { get; } = Encoding.UTF8.GetBytes("SUCCESS");

        private record Schedule(string JobId, string ScheduleId, string CronExpression, bool IsEnabled);

        public Worker(ILogger<Worker> logger, IConfiguration configuration, ISchedulerFactory schedulerFactory)
        {
            _logger = logger;
            _configuration = configuration;
            _scheduler = schedulerFactory.GetScheduler().Result;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            try
            {
                await ReadAllSchedules(stoppingToken);
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

        private async Task ReadAllSchedules(CancellationToken cancellationToken)
        {
            _logger.LogInformation("Loading schedules from database");

            var etlManagerConnectionString = _configuration.GetValue<string>("EtlManagerConnectionString")
                    ?? throw new ArgumentNullException("etlManagerConnectionString", "Connection string cannot be null");

            using var sqlConnection = new SqlConnection(etlManagerConnectionString);
            await sqlConnection.OpenAsync(cancellationToken);

            using var sqlCommand = new SqlCommand("SELECT ScheduleId, JobId, CronExpression, IsEnabled FROM etlmanager.Schedule", sqlConnection);
            var reader = await sqlCommand.ExecuteReaderAsync(cancellationToken);
            var schedules = new List<Schedule>();
            
            // Read schedule objects from the database.
            while (await reader.ReadAsync(cancellationToken))
            {
                try
                {
                    var jobId = reader["JobId"].ToString()!;
                    var scheduleId = reader["ScheduleId"].ToString()!;
                    var cronExpression = reader["CronExpression"].ToString()!;
                    if (!CronExpression.IsValidExpression(cronExpression))
                    {
                        throw new ArgumentException($"Invalid Cron expression {cronExpression} for schedule id {scheduleId}. The schedule was skipped.");
                    }
                    var isEnabled = reader["IsEnabled"] as bool?;
                    var schedule = new Schedule(jobId, scheduleId, cronExpression, isEnabled == true);
                    schedules.Add(schedule);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error parsing schedule object");
                }
            }

            // Clear the scheduler if there were any existing jobs or triggers.
            await _scheduler.Clear(cancellationToken);

            // Iterate the schedules and add them to the scheduler.
            foreach (var schedule in schedules)
            {
                var jobKey = new JobKey(schedule.JobId);
                var triggerKey = new TriggerKey(schedule.ScheduleId);
                var jobDetail = JobBuilder.Create<ExecutionJob>().WithIdentity(jobKey).Build();
                var trigger = TriggerBuilder.Create().WithIdentity(triggerKey).ForJob(jobDetail).WithCronSchedule(schedule.CronExpression).Build();

                if (!await _scheduler.CheckExists(jobKey, cancellationToken))
                {
                    await _scheduler.ScheduleJob(jobDetail, trigger, cancellationToken);
                }
                else
                {
                    await _scheduler.ScheduleJob(trigger, cancellationToken);
                }

                if (!schedule.IsEnabled)
                {
                    await _scheduler.PauseTrigger(triggerKey, cancellationToken);
                }

                var status = schedule.IsEnabled == true ? "Enabled" : "Paused";
                _logger.LogInformation($"Added schedule id {schedule.ScheduleId} for job id {schedule.JobId} with Cron expression {schedule.CronExpression} and status {status}");
            }

            _logger.LogInformation($"Schedules loaded successfully.");
        }

        private async Task ReadNamedPipeAsync(CancellationToken cancellationToken)
        {
            while (true)
            {
                _logger.LogInformation("Started waiting for named pipe connection for incoming commands");

                using var pipeServer = new NamedPipeServerStream("ETL Manager Scheduler", PipeDirection.InOut, NamedPipeServerStream.MaxAllowedServerInstances,
#pragma warning disable CA1416 // Validate platform compatibility
                    PipeTransmissionMode.Message); // Each byte array is transferred as a single message
#pragma warning restore CA1416 // Validate platform compatibility
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
                            await AddScheduleAsync(command, cancellationToken);
                            break;
                        case SchedulerCommand.CommandType.Delete:
                            await RemoveScheduleAsync(command, cancellationToken);
                            break;
                        case SchedulerCommand.CommandType.Pause:
                            await PauseScheduleAsync(command, cancellationToken);
                            break;
                        case SchedulerCommand.CommandType.Resume:
                            await ResumeScheduleAsync(command, cancellationToken);
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
                await ReadAllSchedules(cancellationToken);
                DatabaseReadError = false;
            }
            catch (Exception)
            {
                DatabaseReadError = true;
                throw;
            }
        }

        private async Task ResumeScheduleAsync(SchedulerCommand command, CancellationToken cancellationToken)
        {
            if (command.ScheduleId is null)
                throw new ArgumentNullException(nameof(command.ScheduleId), "Schedule id was null");

            var triggerKey = new TriggerKey(command.ScheduleId);
            await _scheduler.ResumeTrigger(triggerKey, cancellationToken);

            _logger.LogInformation($"Resumed schedule id {command.ScheduleId} for job id {command.JobId}");
        }

        private async Task PauseScheduleAsync(SchedulerCommand command, CancellationToken cancellationToken)
        {
            if (command.ScheduleId is null)
                throw new ArgumentNullException(nameof(command.ScheduleId), "Schedule id was null");

            var triggerKey = new TriggerKey(command.ScheduleId);
            await _scheduler.PauseTrigger(triggerKey, cancellationToken);

            _logger.LogInformation($"Paused schedule id {command.ScheduleId} for job id {command.JobId}");
        }

        private async Task RemoveScheduleAsync(SchedulerCommand command, CancellationToken cancellationToken)
        {
            // If no schedule was mentioned, delete all schedules for the given job.
            if (command.ScheduleId is null)
            {
                if (command.JobId is null)
                    throw new ArgumentNullException(nameof(command.JobId), "Schedule id and job id were null when one of them should be given");

                var jobKey = new JobKey(command.JobId);
                await _scheduler.DeleteJob(jobKey, cancellationToken);

                _logger.LogInformation($"Deleted all schedules for job id {command.JobId}");
            }
            // Otherwise delete only the given schedule.
            else
            {
                var triggerKey = new TriggerKey(command.ScheduleId);
                await _scheduler.UnscheduleJob(triggerKey, cancellationToken);

                _logger.LogInformation($"Deleted schedule id {command.ScheduleId} for job id {command.JobId}");
            }
        }

        private async Task AddScheduleAsync(SchedulerCommand command, CancellationToken cancellationToken)
        {
            if (command.CronExpression is null)
                throw new ArgumentNullException(nameof(command.CronExpression), "Cron expression was null");

            if (command.ScheduleId is null)
                throw new ArgumentNullException(nameof(command.ScheduleId), "Schedule id was null");

            if (command.JobId is null)
                throw new ArgumentNullException(nameof(command.JobId), "Job id was null");

            // Check that the Cron expression is valid.
            if (!CronExpression.IsValidExpression(command.CronExpression))
                throw new ArgumentException($"Invalid Cron expression for schedule id {command.ScheduleId}: {command.CronExpression}");

            var jobKey = new JobKey(command.JobId);
            var jobDetail = await _scheduler.GetJobDetail(jobKey, cancellationToken) ?? JobBuilder.Create<ExecutionJob>().WithIdentity(command.JobId).Build();
            var trigger = TriggerBuilder.Create().WithIdentity(command.ScheduleId).ForJob(jobDetail).WithCronSchedule(command.CronExpression).Build();

            if (!await _scheduler.CheckExists(jobKey, cancellationToken))
            {
                await _scheduler.ScheduleJob(jobDetail, trigger, cancellationToken);
            }
            else
            {
                await _scheduler.ScheduleJob(trigger, cancellationToken);
            }

            _logger.LogInformation($"Added schedule id {command.ScheduleId} for job id {command.JobId} with Cron expression {command.CronExpression}");
        }

    }
}
