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

            try
            {
                await powerBIServiceHelper.RefreshDatasetAsync(GroupId, DatasetId, cancellationToken);
                return new ExecutionResult.Success();
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

        }
    }
}
