using Biflow.DataAccess;
using Biflow.DataAccess.Models;
using Biflow.Executor.Core.Common;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Biflow.Executor.Core.StepExecutor;

internal class DatasetStepExecutor(
    ILogger<DatasetStepExecutor> logger,
    ITokenService tokenService,
    IOptionsMonitor<ExecutionOptions> options,
    DatasetStepExecution step) : IStepExecutor<DatasetStepExecutionAttempt>
{
    private readonly ILogger<DatasetStepExecutor> _logger = logger;
    private readonly ITokenService _tokenService = tokenService;
    private readonly int _pollingIntervalMs = options.CurrentValue.PollingIntervalMs;
    private readonly DatasetStepExecution _step = step;
    private readonly AppRegistration _appRegistration = step.GetAppRegistration()
        ?? throw new ArgumentNullException(nameof(_appRegistration));

    public DatasetStepExecutionAttempt Clone(DatasetStepExecutionAttempt other, int retryAttemptIndex) =>
        new(other, retryAttemptIndex);

    public async Task<Result> ExecuteAsync(DatasetStepExecutionAttempt attempt, ExtendedCancellationTokenSource cancellationTokenSource)
    {
        var cancellationToken = cancellationTokenSource.Token;
        cancellationToken.ThrowIfCancellationRequested();


        // Start dataset refresh.
        try
        {
            await _appRegistration.RefreshDatasetAsync(_tokenService, _step.DatasetGroupId, _step.DatasetId, cancellationToken);
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
                var refresh = await _appRegistration.GetDatasetRefreshStatus(_tokenService, _step.DatasetGroupId, _step.DatasetId, cancellationToken);
                if (refresh?.Status == "Completed")
                {
                    return Result.Success;
                }
                else if (refresh?.Status == "Failed" || refresh?.Status == "Disabled")
                {
                    attempt.AddError(refresh.ServiceExceptionJson);
                    return Result.Failure;
                }
                else
                {
                    await Task.Delay(_pollingIntervalMs, cancellationToken);
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
