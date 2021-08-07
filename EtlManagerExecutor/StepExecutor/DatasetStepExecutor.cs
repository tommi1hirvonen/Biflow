using EtlManagerDataAccess;
using EtlManagerDataAccess.Models;
using Microsoft.EntityFrameworkCore;
using Serilog;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace EtlManagerExecutor
{
    class DatasetStepExecutor : StepExecutorBase
    {
        private DatasetStepExecution Step { get; init; }

        public DatasetStepExecutor(ExecutionConfiguration configuration, DatasetStepExecution step) : base(configuration)
        {
            Step = step;
        }

        public override async Task<ExecutionResult> ExecuteAsync(CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();


            // Start dataset refresh.
            try
            {
                await Step.AppRegistration.RefreshDatasetAsync(Configuration.TokenService, Step.DatasetGroupId, Step.DatasetId, cancellationToken);                
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
                    var refresh = await Step.AppRegistration.GetDatasetRefreshStatus(Configuration.TokenService, Step.DatasetGroupId, Step.DatasetId, cancellationToken);
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
                        await Task.Delay(Configuration.PollingIntervalMs, cancellationToken);
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
