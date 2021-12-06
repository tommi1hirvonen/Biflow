using Dapper;
using EtlManager.DataAccess;
using EtlManager.DataAccess.Models;
using EtlManager.Executor.Core.Common;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace EtlManager.Executor.Core.StepExecutor;

internal class JobStepExecutor : StepExecutorBase
{
    private readonly ILogger<JobStepExecutor> _logger;
    private readonly IExecutionConfiguration _executionConfiguration;
    private readonly IExecutorLauncher _executorLauncher;
    private readonly IDbContextFactory<EtlManagerContext> _dbContextFactory;

    private JobStepExecution Step { get; }

    public JobStepExecutor(
        ILogger<JobStepExecutor> logger,
        IExecutorLauncher executorLauncher,
        IDbContextFactory<EtlManagerContext> dbContextFactory,
        IExecutionConfiguration executionConfiguration,
        JobStepExecution step)
        : base(logger, dbContextFactory, step)
    {
        _logger = logger;
        _executionConfiguration = executionConfiguration;
        _executorLauncher = executorLauncher;
        _dbContextFactory = dbContextFactory;
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
                _logger.LogError(ex, "{ExecutionId} {Step} Error initializing execution for job {jobId}", Step.ExecutionId, Step, Step.JobToExecuteId);
                return Result.Failure("Error initializing job execution: " + ex.Message);
            }
            
            try
            {
                await _executorLauncher.StartExecutorAsync(jobExecutionId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "{ExecutionId} {Step} Error starting executor process for execution {executionId}", Step.ExecutionId, Step, jobExecutionId);
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
                using var context = _dbContextFactory.CreateDbContext();
                var status = await context.Executions
                    .Where(e => e.ExecutionId == jobExecutionId)
                    .Select(e => e.ExecutionStatus)
                    .FirstAsync();
                return status switch
                {
                    ExecutionStatus.Succeeded or ExecutionStatus.Warning => Result.Success(),
                    ExecutionStatus.Failed => Result.Failure("Sub-execution failed"),
                    ExecutionStatus.Stopped => Result.Failure("Sub-execution was stopped"),
                    ExecutionStatus.Suspended => Result.Failure("Sub-execution was suspended"),
                    ExecutionStatus.NotStarted => Result.Failure("Sub-execution failed to start"),
                    ExecutionStatus.Running => Result.Failure($"Sub-execution was finished but its status was reported as {status} after finishing"),
                    _ => Result.Failure("Unhandled sub-execution status"),
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "{ExecutionId} {Step} Error getting sub-execution status for execution id {executionId}", Step.ExecutionId, Step, jobExecutionId);
                return Result.Failure("Error getting sub-execution status");
            }
        }

        return Result.Success();
    }

}
