using EtlManagerUtils;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Quartz;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.IO;
using System.IO.Pipes;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace EtlManagerScheduler
{
    public class Worker : BackgroundService
    {
        private readonly ILogger<Worker> _logger;
        private readonly IConfiguration _configuration;
        private readonly IScheduler _scheduler;

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
            }
            catch (Exception ex)
            {
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
            var counter = 0;
            var pausedCounter = 0;
            while (await reader.ReadAsync(cancellationToken))
            {
                var jobId = reader["JobId"].ToString() ?? throw new ArgumentNullException("jobId", "JobId was null");
                var scheduleId = reader["ScheduleId"].ToString() ?? throw new ArgumentNullException("scheduleId", "ScheduleId was null");
                var cronExpression = reader["CronExpression"].ToString() ?? throw new ArgumentNullException("cronExpression", "CronExpression was null");
                var isEnabled = reader["IsEnabled"] as bool?;
                var jobKey = new JobKey(jobId);
                var triggerKey = new TriggerKey(scheduleId);
                var jobDetail = JobBuilder.Create<ExecutionJob>().WithIdentity(jobKey).Build();
                var trigger = TriggerBuilder.Create().WithIdentity(triggerKey).ForJob(jobDetail).WithCronSchedule(cronExpression).Build();

                if (!await _scheduler.CheckExists(jobKey, cancellationToken))
                {
                    await _scheduler.ScheduleJob(jobDetail, trigger, cancellationToken);
                }
                else
                {
                    await _scheduler.ScheduleJob(trigger, cancellationToken);
                }

                _logger.LogInformation($"Added schedule id {scheduleId} for job id {jobId} with Cron expression {cronExpression}");
                
                if (!isEnabled == true)
                {
                    await _scheduler.PauseTrigger(triggerKey, cancellationToken);
                    _logger.LogInformation($"Paused schedule id {scheduleId}");
                    pausedCounter++;
                }

                counter++;
            }
            _logger.LogInformation($"Schedules loaded successfully. No. of schedules in total: {counter}. Paused schedules: {pausedCounter}");
        }

        private async Task ReadNamedPipeAsync(CancellationToken cancellationToken)
        {
            while (true)
            {
                _logger.LogInformation("Started waiting for named pipe connection for incoming commands");

                var failureBytes = Encoding.UTF8.GetBytes("FAILURE");
                var successBytes = Encoding.UTF8.GetBytes("SUCCESS");

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

                    var command = JsonSerializer.Deserialize<SchedulerCommand>(json) ?? throw new ArgumentNullException("command", "Scheduler command was null");

                    if (command.Type == SchedulerCommand.CommandType.Add)
                    {
                        if (command.CronExpression is null)
                            throw new ArgumentNullException(nameof(command.CronExpression), "Cron expression was null");

                        if (command.ScheduleId is null)
                            throw new ArgumentNullException(nameof(command.ScheduleId), "Schedule id was null");

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
                    else if (command.Type == SchedulerCommand.CommandType.Delete)
                    {
                        // If no schedule was mentioned, delete all schedules for the given job.
                        if (command.ScheduleId is null)
                        {
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
                    else if (command.Type == SchedulerCommand.CommandType.Pause)
                    {
                        if (command.ScheduleId is null)
                            throw new ArgumentNullException(nameof(command.ScheduleId), "Schedule id was null");

                        var triggerKey = new TriggerKey(command.ScheduleId);
                        await _scheduler.PauseTrigger(triggerKey, cancellationToken);

                        _logger.LogInformation($"Paused schedule id {command.ScheduleId} for job id {command.JobId}");
                    }
                    else if (command.Type == SchedulerCommand.CommandType.Resume)
                    {
                        if (command.ScheduleId is null)
                            throw new ArgumentNullException(nameof(command.ScheduleId), "Schedule id was null");

                        var triggerKey = new TriggerKey(command.ScheduleId);
                        await _scheduler.ResumeTrigger(triggerKey, cancellationToken);

                        _logger.LogInformation($"Resumed schedule id {command.ScheduleId} for job id {command.JobId}");
                    }
                    else
                    {
                        pipeServer.Write(failureBytes, 0, failureBytes.Length);
                        throw new ArgumentException($"Invalid command type {command.Type}");
                    }

                    pipeServer.Write(successBytes, 0, successBytes.Length);
                }
                catch (Exception ex)
                {
                    pipeServer.Write(failureBytes, 0, failureBytes.Length);
                    _logger.LogError(ex, "Error reading named pipe command");
                }
            }
        }

    }
}
