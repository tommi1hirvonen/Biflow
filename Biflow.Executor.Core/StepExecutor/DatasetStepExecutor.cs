using Biflow.DataAccess;
using Biflow.DataAccess.Models;
using Biflow.Executor.Core.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Biflow.Executor.Core.StepExecutor;

internal class DatasetStepExecutor : StepExecutorBase
{
    private readonly ILogger<DatasetStepExecutor> _logger;
    private readonly ITokenService _tokenService;
    private readonly int _pollingIntervalMs;

    private DatasetStepExecution Step { get; }

    public DatasetStepExecutor(
        ILogger<DatasetStepExecutor> logger,
        IDbContextFactory<ExecutorDbContext> dbContextFactory,
        ITokenService tokenService,
        IOptionsMonitor<ExecutionOptions> options,
        DatasetStepExecution step)
        : base(logger, dbContextFactory, step)
    {
        _logger = logger;
        _tokenService = tokenService;
        _pollingIntervalMs = options.CurrentValue.PollingIntervalMs;
        Step = step;
    }

    protected override async Task<Result> ExecuteAsync(ExtendedCancellationTokenSource cancellationTokenSource)
    {
        var cancellationToken = cancellationTokenSource.Token;
        cancellationToken.ThrowIfCancellationRequested();


        // Start dataset refresh.
        try
        {
            await Step.AppRegistration.RefreshDatasetAsync(_tokenService, Step.DatasetGroupId, Step.DatasetId, cancellationToken);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error starting dataset refresh");
            return new Failure(ex, "Error starting dataset refresh operation");
        }

        // Wait for 5 seconds before first attempting to get the dataset refresh status.
        await Task.Delay(5000, cancellationToken);

        while (true)
        {
            try
            {
                var refresh = await Step.AppRegistration.GetDatasetRefreshStatus(_tokenService, Step.DatasetGroupId, Step.DatasetId, cancellationToken);
                if (refresh?.Status == "Completed")
                {
                    return new Success();
                }
                else if (refresh?.Status == "Failed" || refresh?.Status == "Disabled")
                {
                    return new Failure(refresh.ServiceExceptionJson);
                }
                else
                {
                    await Task.Delay(_pollingIntervalMs, cancellationToken);
                }
            }
            catch (OperationCanceledException ex)
            {
                return new Cancel(ex);
            }
            catch (Exception ex)
            {
                return new Failure(ex, "Error getting dataset refresh status");
            }
        }
    }
}
