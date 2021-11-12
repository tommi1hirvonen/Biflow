using EtlManager.DataAccess;
using EtlManager.DataAccess.Models;
using Microsoft.EntityFrameworkCore;
using Serilog;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace EtlManager.Executor;

class DatasetStepExecutor : StepExecutorBase
{
    private readonly ITokenService _tokenService;
    private readonly IExecutionConfiguration _executionConfiguration;

    private DatasetStepExecution Step { get; init; }

    public DatasetStepExecutor(
        IDbContextFactory<EtlManagerContext> dbContextFactory,
        ITokenService tokenService,
        IExecutionConfiguration executionConfiguration,
        DatasetStepExecution step)
        : base(dbContextFactory, step)
    {
        _tokenService = tokenService;
        _executionConfiguration = executionConfiguration;
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
            Log.Error(ex, "Error starting dataset refresh");
            return Result.Failure(ex.Message);
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
                    return Result.Success();
                }
                else if (refresh?.Status == "Failed" || refresh?.Status == "Disabled")
                {
                    return Result.Failure(refresh.ServiceExceptionJson);
                }
                else
                {
                    await Task.Delay(_executionConfiguration.PollingIntervalMs, cancellationToken);
                }
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                return Result.Failure(ex.Message);
            }
        }
    }
}
