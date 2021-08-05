using EtlManagerDataAccess;
using EtlManagerDataAccess.Models;
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

            // Get reference to the Power BI Service helper object.
            PowerBIServiceHelper powerBIServiceHelper;
            try
            {
                powerBIServiceHelper = await PowerBIServiceHelper.GetPowerBIServiceHelperAsync(Configuration.DbContextFactory, Step.AppRegistrationId);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error getting App registration information for id {AppRegistrationId}", Step.AppRegistrationId);
                return new ExecutionResult.Failure($"Error getting Power BI Service object information:\n{ex.Message}");
            }

            // Start dataset refresh.
            try
            {
                await powerBIServiceHelper.RefreshDatasetAsync(Step.DatasetGroupId, Step.DatasetId, cancellationToken);
                
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
                    var refresh = await powerBIServiceHelper.GetDatasetRefreshStatus(Step.DatasetGroupId, Step.DatasetId, cancellationToken);
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
