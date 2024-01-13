using Biflow.Core.Entities;
using Biflow.Executor.Core.Common;
using Dapper;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using System.Text.Encodings.Web;
using System.Text.Json;

namespace Biflow.Executor.Core.StepExecutor;

internal class AgentJobStepExecutor(IOptionsMonitor<ExecutionOptions> options, AgentJobStepExecution step)
    : IStepExecutor<AgentJobStepExecutionAttempt>
{
    private readonly int _pollingIntervalMs = options.CurrentValue.PollingIntervalMs;
    private readonly JsonSerializerOptions _serializerOptions =
        new() { WriteIndented = true, Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping };
    private readonly AgentJobStepExecution _step = step;
    private readonly SqlConnectionInfo _connection = step.GetConnection()
        ?? throw new ArgumentNullException(nameof(_connection));

    public AgentJobStepExecutionAttempt Clone(AgentJobStepExecutionAttempt other, int retryAttemptIndex) =>
        new(other, retryAttemptIndex);

    public async Task<Result> ExecuteAsync(AgentJobStepExecutionAttempt attempt, ExtendedCancellationTokenSource cancellationTokenSource)
    {
        var cancellationToken = cancellationTokenSource.Token;
        cancellationToken.ThrowIfCancellationRequested();

        var connectionString = _connection.ConnectionString;

        // Start agent job execution
        try
        {
            using var connection = new SqlConnection(connectionString);
            await connection.ExecuteAsync(
                "EXEC msdb.dbo.sp_start_job @job_name = @AgentJobName",
                new { _step.AgentJobName });
        }
        catch (Exception ex)
        {
            attempt.AddError(ex, "Error starting agent job");
            return Result.Failure;
        }

        using var timeoutCts = _step.TimeoutMinutes > 0
            ? new CancellationTokenSource(TimeSpan.FromMinutes(_step.TimeoutMinutes))
            : new CancellationTokenSource();
        using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, timeoutCts.Token);

        // Monitor the agent job's status
        int? historyId = null;
        try
        {
            while (historyId is null)
            {
                await Task.Delay(_pollingIntervalMs, linkedCts.Token);
                using var connection = new SqlConnection(connectionString);
                // [sp_help_jobactivity] returns one row describing the agent job's status.
                // Column [job_history_id] will contain the history id of the agent job outcome when it has completed.
                var status = await connection.QueryAsync<dynamic>(
                    "EXEC msdb.dbo.sp_help_jobactivity @job_name = @AgentJobName",
                    new { _step.AgentJobName });
                historyId = status.FirstOrDefault()?.job_history_id;
            }
        }
        catch (OperationCanceledException ex)
        {
            using var connection = new SqlConnection(connectionString);
            await connection.ExecuteAsync(
                "EXEC msdb.dbo.sp_stop_job @job_name = @AgentJobName",
                new { _step.AgentJobName });
            if (timeoutCts.IsCancellationRequested)
            {
                attempt.AddError(ex, "Step execution timed out");
                return Result.Failure;
            }
            attempt.AddWarning(ex);
            return Result.Cancel;
        }
        catch (Exception ex)
        {
            attempt.AddError(ex, "Error monitoring agent job execution status");
            return Result.Failure;
        }

        try
        {
            using var connection = new SqlConnection(connectionString);

            // Get the agent job outcome status using the history id.
            var status = await connection.ExecuteScalarAsync<int>(
                "SELECT run_status FROM msdb.dbo.sysjobhistory WHERE instance_id = @InstanceId",
                new { InstanceId = historyId });

            // Get data for all steps belonging to this agent job execution (including the job outcome).
            var messageRows = await connection.QueryAsync<dynamic>("""
                    SELECT
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
                    ORDER BY a.instance_id
                    """,
                new { InstanceId = historyId });

            var messageString = JsonSerializer.Serialize(messageRows, _serializerOptions);
            string? jobOutcome = messageRows.LastOrDefault()?.message;

            // 0 = Failed, 1 = Succeeded, 2 = Retry, 3 = Canceled, 4 = In Progress
            if (status == 1)
            {
                attempt.AddOutput(messageString);
                return Result.Success;
            }
            else if (status == 0 || status == 3)
            {
                attempt.AddError(messageString);
                return Result.Failure;
            }
            else
            {
                attempt.AddOutput(messageString);
                attempt.AddError($"Unexpected agent job history run status ({status}) after execution.\n{jobOutcome}");
                return Result.Failure;
            }
        }
        catch (Exception ex)
        {
            attempt.AddError(ex, "Error getting agent job status and message from msdb.dbo.sysjobhistory");
            return Result.Failure;
        }
    }

}
