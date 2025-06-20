﻿using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Biflow.Executor.Core.StepExecutor;

[UsedImplicitly]
internal class DatasetStepExecutor(
    ILogger<DatasetStepExecutor> logger,
    IDbContextFactory<ExecutorDbContext> dbContextFactory,
    IOptionsMonitor<ExecutionOptions> options,
    ITokenService tokenService)
    : StepExecutor<DatasetStepExecution, DatasetStepExecutionAttempt>(logger, dbContextFactory)
{
    private readonly ILogger<DatasetStepExecutor> _logger = logger;
    private readonly int _pollingIntervalMs = options.CurrentValue.PollingIntervalMs;
    private readonly ITokenService _tokenService = tokenService;

    protected override async Task<Result> ExecuteAsync(
        OrchestrationContext context,
        DatasetStepExecution step,
        DatasetStepExecutionAttempt attempt,
        ExtendedCancellationTokenSource cancellationTokenSource)
    {
        var cancellationToken = cancellationTokenSource.Token;
        cancellationToken.ThrowIfCancellationRequested();

        var client = step.GetAzureCredential()?.CreateDatasetClient(_tokenService);
        ArgumentNullException.ThrowIfNull(client);

        // Start dataset refresh.
        try
        {
            await client.RefreshDatasetAsync(step.WorkspaceId, step.DatasetId, cancellationToken);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error starting dataset refresh");
            attempt.AddError(ex, "Error starting dataset refresh operation");
            return Result.Failure;
        }

        // Wait for 5 seconds before first attempting to get the dataset refresh status.
        await Task.Delay(5000, cancellationToken);

        while (true)
        {
            try
            {
                var (status, refresh) = await client.GetDatasetRefreshStatusAsync(step.WorkspaceId, step.DatasetId,
                    cancellationToken);
                switch (status)
                {
                    case DatasetRefreshStatus.Completed:
                        return Result.Success;
                    case DatasetRefreshStatus.Failed or DatasetRefreshStatus.Disabled:
                        attempt.AddError(refresh?.ServiceExceptionJson);
                        return Result.Failure;
                    default:
                        await Task.Delay(_pollingIntervalMs, cancellationToken);
                        break;
                }
            }
            catch (OperationCanceledException ex)
            {
                // If the token was canceled, report result as 'Cancel'.
                if (cancellationToken.IsCancellationRequested)
                {
                    attempt.AddWarning(ex);
                    return Result.Cancel;
                }
                // If not, report error. This means the step was not canceled, but instead the DatasetClient's
                // underlying HttpClient might have timed out.
                attempt.AddError(ex);
                return Result.Failure;
            }
            catch (Exception ex)
            {
                attempt.AddError(ex, "Error getting dataset refresh status");
                return Result.Failure;
            }
        }
    }
}
