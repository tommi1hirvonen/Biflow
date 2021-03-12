using Microsoft.PowerBI.Api;
using Microsoft.Rest;
using Serilog;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace EtlManagerExecutor.StepExecution
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

            PowerBIService powerBIService;
            try
            {
                powerBIService = await PowerBIService.GetPowerBIServiceAsync(Configuration.ConnectionString, PowerBIServiceId, Configuration.EncryptionKey);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error getting Power BI Service information for id {PowerBIServiceId}", PowerBIServiceId);
                throw;
            }

            await powerBIService.CheckAccessTokenValidityAsync(Configuration.ConnectionString);

            
            var credentials = new TokenCredentials(powerBIService.AccessToken);
            var client = new PowerBIClient(credentials);

            try
            {
                await client.Datasets.RefreshDatasetInGroupAsync(Guid.Parse(GroupId), DatasetId, cancellationToken: cancellationToken);
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
