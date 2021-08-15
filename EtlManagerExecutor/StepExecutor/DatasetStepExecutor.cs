using EtlManagerDataAccess;
using EtlManagerDataAccess.Models;
using Microsoft.EntityFrameworkCore;
using Serilog;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace EtlManagerExecutor
{
    class DatasetStepExecutor : IStepExecutor
    {
        private readonly ITokenService _tokenService;
        private readonly IExecutionConfiguration _executionConfiguration;

        private DatasetStepExecution Step { get; init; }

        public int RetryAttemptCounter { get; set; } = 0;

        public DatasetStepExecutor(ITokenService tokenService, IExecutionConfiguration executionConfiguration, DatasetStepExecution step)
        {
            _tokenService = tokenService;
            _executionConfiguration = executionConfiguration;
            Step = step;
        }

        public async Task<ExecutionResult> ExecuteAsync(ExtendedCancellationTokenSource cancellationTokenSource)
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
                return new ExecutionResult.Failure(ex.Message);
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
                        return new ExecutionResult.Success();
                    }
                    else if (refresh?.Status == "Failed" || refresh?.Status == "Disabled")
                    {
                        return new ExecutionResult.Failure(refresh.ServiceExceptionJson);
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
                    return new ExecutionResult.Failure(ex.Message);
                }
            }
        }
    }
}
