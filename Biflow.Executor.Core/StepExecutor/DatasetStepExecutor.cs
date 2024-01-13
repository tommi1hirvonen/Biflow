using Biflow.Core.Entities;
using Biflow.Core.Interfaces;
using Biflow.Executor.Core.Common;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Biflow.Executor.Core.StepExecutor;

internal class DatasetStepExecutor(
    ILogger<DatasetStepExecutor> logger,
    IOptionsMonitor<ExecutionOptions> options,
    ITokenService tokenService,
    DatasetStepExecution step) : IStepExecutor<DatasetStepExecutionAttempt>
{
    private readonly ILogger<DatasetStepExecutor> _logger = logger;
    private readonly int _pollingIntervalMs = options.CurrentValue.PollingIntervalMs;
    private readonly DatasetStepExecution _step = step;
    private readonly DatasetClient _datasetClient = step.GetAppRegistration()?.CreateDatasetClient(tokenService)
        ?? throw new ArgumentNullException(nameof(_datasetClient));

    public DatasetStepExecutionAttempt Clone(DatasetStepExecutionAttempt other, int retryAttemptIndex) =>
        new(other, retryAttemptIndex);

    public async Task<Result> ExecuteAsync(DatasetStepExecutionAttempt attempt, ExtendedCancellationTokenSource cancellationTokenSource)
    {
        var cancellationToken = cancellationTokenSource.Token;
        cancellationToken.ThrowIfCancellationRequested();


        // Start dataset refresh.
        try
        {
            await _datasetClient.RefreshDatasetAsync(_step.DatasetGroupId, _step.DatasetId, cancellationToken);
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
                var (status, refresh) = await _datasetClient.GetDatasetRefreshStatusAsync(_step.DatasetGroupId, _step.DatasetId, cancellationToken);
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
                attempt.AddWarning(ex);
                return Result.Cancel;
            }
            catch (Exception ex)
            {
                attempt.AddError(ex, "Error getting dataset refresh status");
                return Result.Failure;
            }
        }
    }
}
