using System.Text.Json;
using Biflow.Executor.Core.Common;
using Dapper;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;

namespace Biflow.Executor.Core.StepExecutor;

internal class ScdStepExecutor(
    ILogger<ScdStepExecutor> logger,
    IDbContextFactory<ExecutorDbContext> dbContextFactory)
    : StepExecutor<ScdStepExecution, ScdStepExecutionAttempt>(logger, dbContextFactory)
{
    private readonly ILogger<ScdStepExecutor> _logger = logger;
    
    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };
    
    protected override async Task<Result> ExecuteAsync(
        ScdStepExecution step,
        ScdStepExecutionAttempt attempt,
        ExtendedCancellationTokenSource cancellationTokenSource)
    {
        var cancellationToken = cancellationTokenSource.Token;
        cancellationToken.ThrowIfCancellationRequested();

        var scdTable = step.GetScdTable();
        ArgumentNullException.ThrowIfNull(scdTable);
        
        var scdTableJson = JsonSerializer.Serialize(scdTable, JsonOptions);
        attempt.AddOutput($"SCD table configuration:\n{scdTableJson}");
        
        // TODO: Handle impersonation for MsSqlConnections

        var scdProvider = new MsSqlScdProvider(scdTable, new MsSqlColumnMetadataProvider());

        string structureUpdateStatement;
        try
        {
            structureUpdateStatement = await scdProvider.CreateStructureUpdateStatementAsync(cancellationToken);
            if (!string.IsNullOrWhiteSpace(structureUpdateStatement))
                attempt.AddOutput(structureUpdateStatement);
        }
        catch (Exception ex)
        {
            attempt.AddError(ex, "Error getting SCD table structure update statement");
            return Result.Failure;
        }

        string dataLoadStatement;
        try
        {
            dataLoadStatement = await scdProvider.CreateDataLoadStatementAsync(cancellationToken);
            attempt.AddOutput(dataLoadStatement);
        }
        catch (Exception ex)
        {
            attempt.AddError(ex, "Error getting SCD table data load statement");
            return Result.Failure;
        }

        if (!string.IsNullOrWhiteSpace(structureUpdateStatement))
        {
            try
            {
                // command timeout = 0 => wait indefinitely
                var command = new CommandDefinition(
                    structureUpdateStatement,
                    commandTimeout: Convert.ToInt32(step.TimeoutMinutes * 60),
                    cancellationToken: cancellationToken);
                await using var connection = new SqlConnection(scdTable.Connection.ConnectionString);
                connection.InfoMessage += (_, eventArgs) => attempt.AddOutput(eventArgs.Message);
                await connection.ExecuteAsync(command);
            }
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

                _logger.LogWarning(ex, "{ExecutionId} {Step} SCD table structure update execution failed", step.ExecutionId, step);

                return Result.Failure;
            }
            catch (Exception ex)
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    attempt.AddWarning(ex);
                    return Result.Cancel;
                }
                attempt.AddError(ex, "SCD table structure update execution failed");
                _logger.LogWarning(ex, "{ExecutionId} {Step} SCD table structure update execution failed", step.ExecutionId, step);
                return Result.Failure;
            }
        }

        try
        {
            var command = new CommandDefinition(
                dataLoadStatement,
                commandTimeout: Convert.ToInt32(step.TimeoutMinutes * 60),
                cancellationToken: cancellationToken);
            await using var connection = new SqlConnection(scdTable.Connection.ConnectionString);
            connection.InfoMessage += (_, eventArgs) => attempt.AddOutput(eventArgs.Message);
            await connection.ExecuteAsync(command);
        }
        catch (SqlException ex)
        {
            var errors = ex.Errors.Cast<SqlError>();
            foreach (var error in errors)
            {
                var message = $"Line: {error.LineNumber}\nMessage: {error.Message}";
                attempt.AddError(ex, message);
            }

            if (cancellationToken.IsCancellationRequested)
            {
                attempt.AddWarning(ex);
                return Result.Cancel;
            }

            _logger.LogWarning(ex, "{ExecutionId} {Step} SCD table data load execution failed", step.ExecutionId, step);

            return Result.Failure;
        }
        catch (Exception ex)
        {
            if (cancellationToken.IsCancellationRequested)
            {
                attempt.AddWarning(ex);
                return Result.Cancel;
            }
            attempt.AddError(ex, "SCD table data load execution failed");
            _logger.LogWarning(ex, "{ExecutionId} {Step} SCD table data load execution failed", step.ExecutionId, step);
            return Result.Failure;
        }
        
        return Result.Success;
    }
}