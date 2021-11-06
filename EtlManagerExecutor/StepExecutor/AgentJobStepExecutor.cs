using Dapper;
using EtlManagerDataAccess;
using EtlManagerDataAccess.Models;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Encodings.Web;
using System.Threading;
using System.Threading.Tasks;

namespace EtlManagerExecutor
{
    class AgentJobStepExecutor : StepExecutorBase
    {
        private readonly IExecutionConfiguration _executionConfiguration;

        private AgentJobStepExecution Step { get; }

        public AgentJobStepExecutor(
            IExecutionConfiguration executionConfiguration,
            IDbContextFactory<EtlManagerContext> dbContextFactory,
            AgentJobStepExecution step) : base(dbContextFactory, step)
        {
            _executionConfiguration = executionConfiguration;
            Step = step;
        }

        protected override async Task<Result> ExecuteAsync(ExtendedCancellationTokenSource cancellationTokenSource)
        {
            var cancellationToken = cancellationTokenSource.Token;
            cancellationToken.ThrowIfCancellationRequested();

            var connectionString = Step.Connection.ConnectionString;

            // Start agent job execution
            try
            {
                using var connection = new SqlConnection(connectionString);
                await connection.ExecuteAsync(
                    "EXEC msdb.dbo.sp_start_job @job_name = @AgentJobName",
                    new { Step.AgentJobName });
            }
            catch (Exception ex)
            {
                return Result.Failure($"Error starting agent job: {ex.Message}");
            }

            using var timeoutCts = Step.TimeoutMinutes > 0
                ? new CancellationTokenSource(TimeSpan.FromMinutes(Step.TimeoutMinutes))
                : new CancellationTokenSource();
            using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, timeoutCts.Token);

            // Monitor the agent job's status
            int? historyId = null;
            try
            {
                while (historyId is null)
                {
                    await Task.Delay(_executionConfiguration.PollingIntervalMs, linkedCts.Token);
                    using var connection = new SqlConnection(connectionString);
                    // [sp_help_jobactivity] returns one row describing the agent job's status.
                    // Column [job_history_id] will contain the history id of the agent job outcome when it has completed.
                    var status = await connection.QueryAsync<dynamic>(
                        "EXEC msdb.dbo.sp_help_jobactivity @job_name = @AgentJobName",
                        new { Step.AgentJobName });
                    historyId = status.FirstOrDefault()?.job_history_id;
                }
            }
            catch (OperationCanceledException)
            {
                using var connection = new SqlConnection(connectionString);
                await connection.ExecuteAsync(
                    "EXEC msdb.dbo.sp_stop_job @job_name = @AgentJobName",
                    new { Step.AgentJobName });
                if (timeoutCts.IsCancellationRequested)
                {
                    return Result.Failure("Step execution timed out"); // Report failure => allow possible retries
                }
                throw; // Step was canceled => pass the exception => no retries
            }
            catch (Exception ex)
            {
                return Result.Failure($"Error monitoring agent job execution status:\n{ex.Message}");
            }

            try
            {
                using var connection = new SqlConnection(connectionString);
                
                // Get the agent job outcome status using the history id.
                var status = await connection.ExecuteScalarAsync<int>(
                    "SELECT run_status FROM msdb.dbo.sysjobhistory WHERE instance_id = @InstanceId",
                    new { InstanceId = historyId });

                // Get data for all steps belonging to this agent job execution (including the job outcome).
                var messageRows = await connection.QueryAsync<dynamic>(
                    @"SELECT
	                    a.instance_id,
	                    a.step_id,
	                    a.step_name,
	                    a.message,
	                    a.run_status,
	                    a.run_date,
	                    a.run_time,
	                    a.run_duration,
	                    a.retries_attempted,
	                    a.server
                    FROM msdb.dbo.sysjobhistory AS a
                    	INNER JOIN msdb.dbo.sysjobhistory AS b ON b.instance_id = @InstanceId
                    WHERE a.instance_id <= b.instance_id AND
	                    a.run_date >= b.run_date AND
	                    a.run_time >= b.run_time
                    ORDER BY a.instance_id",
                    new { InstanceId = historyId });

                var options = new JsonSerializerOptions
                {
                    WriteIndented = true,
                    Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
                };
                var messageString = JsonSerializer.Serialize(messageRows, options);
                string? jobOutcome = messageRows.LastOrDefault()?.message;

                // 0 = Failed, 1 = Succeeded, 2 = Retry, 3 = Canceled, 4 = In Progress
                if (status == 1)
                {
                    return Result.Success(messageString);
                }
                else if (status == 0 || status == 3)
                {
                    return Result.Failure(messageString);
                }
                else
                {
                    return Result.Failure($"Unexpected agent job history run status ({status}) after execution.\n{jobOutcome}");
                }
            }
            catch (Exception ex)
            {
                return Result.Failure($"Error getting agent job status and message from msdb.dbo.sysjobhistory:\n{ex.Message}");
            }
        }

    }
}
