using Biflow.DataAccess;
using Biflow.DataAccess.Models;
using Biflow.Executor.Core.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Biflow.Executor.Core.StepExecutor;

internal class DatasetStepExecutor(
    ILogger<DatasetStepExecutor> logger,
    IDbContextFactory<ExecutorDbContext> dbContextFactory,
    ITokenService tokenService,
    IOptionsMonitor<ExecutionOptions> options,
    DatasetStepExecution step) : StepExecutorBase(logger, dbContextFactory, step)
{
    private readonly ILogger<DatasetStepExecutor> _logger = logger;
    private readonly ITokenService _tokenService = tokenService;
    private readonly int _pollingIntervalMs = options.CurrentValue.PollingIntervalMs;
    private readonly DatasetStepExecution _step = step;

    protected override async Task<Result> ExecuteAsync(ExtendedCancellationTokenSource cancellationTokenSource)
    {
        var cancellationToken = cancellationTokenSource.Token;
        cancellationToken.ThrowIfCancellationRequested();


        // Start dataset refresh.
        try
        {
            await _step.AppRegistration.RefreshDatasetAsync(_tokenService, _step.DatasetGroupId, _step.DatasetId, cancellationToken);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error starting dataset refresh");
            AddError(ex, "Error starting dataset refresh operation");
            return Result.Failure;
        }

        // Wait for 5 seconds before first attempting to get the dataset refresh status.
        await Task.Delay(5000, cancellationToken);

        while (true)
        {
            try
            {
                var refresh = await _step.AppRegistration.GetDatasetRefreshStatus(_tokenService, _step.DatasetGroupId, _step.DatasetId, cancellationToken);
                if (refresh?.Status == "Completed")
                {
                    return Result.Success;
                }
                else if (refresh?.Status == "Failed" || refresh?.Status == "Disabled")
                {
                    AddError(refresh.ServiceExceptionJson);
                    return Result.Failure;
                }
                else
                {
                    await Task.Delay(_pollingIntervalMs, cancellationToken);
                }
            }
            catch (OperationCanceledException ex)
            {
                AddWarning(ex);
                return Result.Cancel;
            }
            catch (Exception ex)
            {
                AddError(ex, "Error getting dataset refresh status");
                return Result.Failure;
            }
        }
    }
}
