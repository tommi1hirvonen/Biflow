using EtlManagerUtils;
using Serilog;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace EtlManagerExecutor
{
    class DatasetStepExecution : StepExecutionBase
    {
        private string PowerBIServiceId { get; init; }
        private string GroupId { get; init; }
        private string DatasetId { get; init; }

        public DatasetStepExecution(ExecutionConfiguration configuration, Step step, string powerBIServiceId,
            string groupId, string datasetId) : base(configuration, step)
        {
            PowerBIServiceId = powerBIServiceId;
            GroupId = groupId;
            DatasetId = datasetId;
        }

        public override async Task<ExecutionResult> ExecuteAsync(CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (Configuration.EncryptionKey is null)
                throw new ArgumentNullException(nameof(Configuration.EncryptionKey), "Encryption key cannot be null for dataset step executions");

            // Get reference to the Power BI Service helper object.
            PowerBIServiceHelper powerBIServiceHelper;
            try
            {
                powerBIServiceHelper = await PowerBIServiceHelper.GetPowerBIServiceHelperAsync(Configuration.ConnectionString, PowerBIServiceId, Configuration.EncryptionKey);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error getting Power BI Service information for id {PowerBIServiceId}", PowerBIServiceId);
                return new ExecutionResult.Failure($"Error getting Power BI Service object information:\n{ex.Message}");
            }

            // Start dataset refresh.
            try
            {
                await powerBIServiceHelper.RefreshDatasetAsync(GroupId, DatasetId, cancellationToken);
                
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
                    var refresh = await powerBIServiceHelper.GetDatasetRefreshStatus(GroupId, DatasetId, cancellationToken);
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
