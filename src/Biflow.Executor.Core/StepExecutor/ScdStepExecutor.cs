using System.Text.Json;
using Biflow.Core.Entities.Scd;
using Dapper;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;
using Snowflake.Data.Client;

namespace Biflow.Executor.Core.StepExecutor;

[UsedImplicitly]
internal class ScdStepExecutor(
    ILogger<ScdStepExecutor> logger,
    IDbContextFactory<ExecutorDbContext> dbContextFactory,
    ScdStepExecution step,
    ScdStepExecutionAttempt attempt) : IStepExecutor
{
    private readonly ScdTable _scdTable =
        step.GetScdTable() ?? throw new ArgumentNullException(message: "SCD table was null", innerException: null);
    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };
    
    public async Task<Result> ExecuteAsync(OrchestrationContext context, ExtendedCancellationTokenSource cts)
    {
        cts.Token.ThrowIfCancellationRequested();
        
        var scdTableJson = JsonSerializer.Serialize(_scdTable, JsonOptions);
        attempt.AddOutput($"SCD table configuration:\n{scdTableJson}");

        var scdProvider = _scdTable.CreateScdProvider();
        
        // Use a common timeout cancellation token source => one timeout value applies to the whole SCD load process.
        using var timeoutCts = step.TimeoutMinutes > 0
            ? new CancellationTokenSource(TimeSpan.FromMinutes(step.TimeoutMinutes))
            : new CancellationTokenSource();
        using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(
            cts.Token, timeoutCts.Token);
        var cancellationToken = linkedCts.Token;

        string structureUpdateStatement;
        try
        {
            structureUpdateStatement = await scdProvider.CreateStructureUpdateStatementAsync(cancellationToken);
        }
        catch (ScdTableValidationException ex)
        {
            attempt.AddError(ex, ex.Message);
            return Result.Failure;
        }
        catch (Exception ex)
        {
            attempt.AddError(ex, "Error getting SCD target table structure update statement");
            return Result.Failure;
        }

        if (!string.IsNullOrWhiteSpace(structureUpdateStatement))
        {
            attempt.AddOutput(structureUpdateStatement);
            // Update the output messages so that they are visible in monitoring even during execution.
            await UpdateOutputAsync(cancellationToken);
            var structureUpdateResult = await ExecuteStatementAsync(structureUpdateStatement, cancellationToken);
            if (structureUpdateResult is not null)
            {
                // result is not null only if the statement did not succeed
                return structureUpdateResult;
            }
        }

        string stagingLoadStatement;
        IReadOnlyList<IOrderedLoadColumn> sourceColumns;
        IReadOnlyList<IOrderedLoadColumn> targetColumns;
        try
        {
            (stagingLoadStatement, sourceColumns, targetColumns) =
                await scdProvider.CreateStagingLoadStatementAsync(cancellationToken);
            attempt.AddOutput(stagingLoadStatement);
            // Update the output messages so that they are visible in monitoring even during execution.
            await UpdateOutputAsync(cancellationToken);
        }
        catch (ScdTableValidationException ex)
        {
            attempt.AddError(ex, ex.Message);
            return Result.Failure;
        }
        catch (Exception ex)
        {
            attempt.AddError(ex, "Error getting SCD staging table data load statement");
            return Result.Failure;
        }
        
        var stagingLoadResult = await ExecuteStatementAsync(stagingLoadStatement, cancellationToken);
        if (stagingLoadResult is not null)
        {
            // result is not null only if the statement did not succeed
            return stagingLoadResult;
        }

        string targetLoadStatement;
        try
        {
            // Reuse columns from before since they have not changed.
            targetLoadStatement = scdProvider.CreateTargetLoadStatement(sourceColumns, targetColumns);
            attempt.AddOutput(targetLoadStatement);
            // Update the output messages so that they are visible in monitoring even during execution.
            await UpdateOutputAsync(cancellationToken);
        }
        catch (ScdTableValidationException ex)
        {
            attempt.AddError(ex, ex.Message);
            return Result.Failure;
        }
        catch (Exception ex)
        {
            attempt.AddError(ex, "Error getting SCD target table data load statement");
            return Result.Failure;
        }
        
        var dataLoadResult = await ExecuteStatementAsync(targetLoadStatement, cancellationToken);
        
        return dataLoadResult ?? Result.Success;
    }

    private async Task<Result?> ExecuteStatementAsync(string statement, CancellationToken cancellationToken)
    {
        try
        {
            var command = new CommandDefinition(statement, commandTimeout: 0, cancellationToken: cancellationToken);
            switch (_scdTable.Connection)
            {
                case MsSqlConnection msSql:
                {
                    if (msSql.Credential is not null && !OperatingSystem.IsWindows())
                    {
                        attempt.AddWarning("Connection has impersonation enabled but the OS platform does not support it. Impersonation will be skipped.");
                    }
                    await using var connection = msSql.CreateDbConnection((_, eventArgs) =>
                        attempt.AddOutput(eventArgs.Message));
                    await msSql.RunImpersonatedOrAsCurrentUserAsync(
                        () => connection.ExecuteAsync(command));
                    break;
                }
                default:
                {
                    await using var connection = _scdTable.Connection.CreateDbConnection();
                    await connection.ExecuteAsync(command);
                    break;
                }
            }
            return null;
        }
        // MS SQL error
        catch (SqlException ex)
        {
            var errors = ex.Errors.Cast<SqlError>();
            foreach (var error in errors)
            {
                var message = $"Line: {error.LineNumber}\nMessage: {error.Message}";
                attempt.AddError(ex, message);
            }

            // Return Cancel if the SqlCommand failed due to cancel being requested.
            // Underlying ExecuteNonQueryAsync() throws SqlException in case cancel was requested.
            if (cancellationToken.IsCancellationRequested)
            {
                attempt.AddWarning(ex);
                return Result.Cancel;
            }

            logger.LogWarning(ex, "{ExecutionId} {Step} SCD table execution failed", step.ExecutionId, step);

            return Result.Failure;
        }
        // Snowflake error
        catch (SnowflakeDbException ex)
        {
            attempt.AddError(ex, ex.Message);

            // Return Cancel if the SqlCommand failed due to cancel being requested.
            // Underlying ExecuteNonQueryAsync() throws SqlException in case cancel was requested.
            if (cancellationToken.IsCancellationRequested)
            {
                attempt.AddWarning(ex);
                return Result.Cancel;
            }

            logger.LogWarning(ex, "{ExecutionId} {Step} SCD table execution failed", step.ExecutionId, step);

            return Result.Failure;
        }
        // Other error
        catch (Exception ex)
        {
            if (cancellationToken.IsCancellationRequested)
            {
                attempt.AddWarning(ex);
                return Result.Cancel;
            }
            attempt.AddError(ex, "SCD table execution failed");
            logger.LogWarning(ex, "{ExecutionId} {Step} SCD table execution failed", step.ExecutionId, step);
            return Result.Failure;
        }
    }

    private async Task UpdateOutputAsync(CancellationToken cancellationToken)
    {
        try
        {
            await using var context = await dbContextFactory.CreateDbContextAsync(cancellationToken);
            await context.StepExecutionAttempts
                .Where(x => x.ExecutionId == attempt.ExecutionId
                            && x.StepId == attempt.StepId
                            && x.RetryAttemptIndex == attempt.RetryAttemptIndex)
                .ExecuteUpdateAsync(x => x.SetProperty(p => p.InfoMessages, attempt.InfoMessages), cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "{executionId} {stepId} Error updating SCD step execution attempt output",
                attempt.ExecutionId, attempt.StepId);
        }
    }
}