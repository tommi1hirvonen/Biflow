using Dapper;
using EtlManager.DataAccess;
using EtlManager.DataAccess.Models;
using EtlManager.Executor.Core.Common;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Serilog;

namespace EtlManager.Executor.Core.StepExecutor;

internal class JobStepExecutor : StepExecutorBase
{
    private readonly IExecutionConfiguration _executionConfiguration;
    private readonly IExecutorLauncher _executorLauncher;

    private JobStepExecution Step { get; }

    public JobStepExecutor(
        IExecutorLauncher executorLauncher,
        IDbContextFactory<EtlManagerContext> dbContextFactory,
        IExecutionConfiguration executionConfiguration,
        JobStepExecution step)
        : base(dbContextFactory, step)
    {
        _executionConfiguration = executionConfiguration;
        _executorLauncher = executorLauncher;
        Step = step;
    }

    protected override async Task<Result> ExecuteAsync(ExtendedCancellationTokenSource cancellationTokenSource)
    {
        var cancellationToken = cancellationTokenSource.Token;
        cancellationToken.ThrowIfCancellationRequested();

        Guid jobExecutionId;

        using (var sqlConnection = new SqlConnection(_executionConfiguration.ConnectionString))
        {
            await sqlConnection.OpenAsync(CancellationToken.None);

            try
            {
                jobExecutionId = await sqlConnection.ExecuteScalarAsync<Guid>(
                    "EXEC etlmanager.ExecutionInitialize @JobId = @JobId_, @Notify = @Notify_, @NotifyCaller = @NotifyCaller_, @NotifyCallerOvertime = @NotifyCallerOvertime_",
                    new
                    {
                        JobId_ = Step.JobToExecuteId,
                        Notify_ = Step.Execution.Notify,
                        NotifyCaller_ = Step.Execution.NotifyCaller,
                        NotifyCallerOvertime_ = Step.Execution.NotifyCallerOvertime
                    });
            }
            catch (Exception ex)
            {
                Log.Error(ex, "{ExecutionId} {Step} Error initializing execution for job {jobId}", Step.ExecutionId, Step, Step.JobToExecuteId);
                return Result.Failure("Error initializing job execution: " + ex.Message);
            }
            
            try
            {
                await _executorLauncher.StartExecutorAsync(jobExecutionId);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "{ExecutionId} {Step} Error starting executor process for execution {executionId}", Step.ExecutionId, Step, jobExecutionId);
                return Result.Failure("Error starting executor process: " + ex.Message);
            }

        }

        if (Step.JobExecuteSynchronized)
        {
            try
            {
                await _executorLauncher.WaitForExitAsync(jobExecutionId, cancellationToken);
            }
            catch (OperationCanceledException)
            {
                await _executorLauncher.CancelAsync(jobExecutionId, cancellationTokenSource.Username);
                throw;
            }

            try
            {
                using var sqlConnection = new SqlConnection(_executionConfiguration.ConnectionString);
                var status = await sqlConnection.ExecuteScalarAsync<string>(
                    "SELECT TOP 1 ExecutionStatus FROM etlmanager.Execution WHERE ExecutionId = @ExecutionId",
                    new { ExecutionId = jobExecutionId });
                return status switch
                {
                    "SUCCEEDED" or "WARNING" => Result.Success(),
                    "FAILED" => Result.Failure("Sub-execution failed"),
                    "STOPPED" => Result.Failure("Sub-execution was stopped"),
                    "SUSPENDED" => Result.Failure("Sub-execution was suspended"),
                    "NOT STARTED" => Result.Failure("Sub-execution failed to start"),
                    "RUNNING" => Result.Failure("Sub-execution was finished but its status was reported as RUNNING after finishing"),
                    _ => Result.Failure("Unhandled sub-execution status"),
                };
            }
            catch (Exception ex)
            {
                Log.Error(ex, "{ExecutionId} {Step} Error getting sub-execution status for execution id {executionId}", Step.ExecutionId, Step, jobExecutionId);
                return Result.Failure("Error getting sub-execution status");
            }
        }

        return Result.Success();
    }

}
