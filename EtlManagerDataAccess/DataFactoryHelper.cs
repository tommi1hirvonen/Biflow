using Dapper;
using EtlManagerDataAccess.Models;
using Microsoft.Azure.Management.DataFactory;
using Microsoft.Azure.Management.DataFactory.Models;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Clients.ActiveDirectory;
using Microsoft.Rest;
using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace EtlManagerDataAccess
{
    public class DataFactoryHelper : AzureHelperBase
    {
        private const string ResourceUrl = "https://management.azure.com/";

        private DataFactory DataFactory { get; init; }

        private DataFactoryHelper(
            DataFactory dataFactory,
            string? accessToken,
            DateTime? accessTokenExpiresOn,
            string connectionString
            ) : base(dataFactory.AppRegistration, accessToken, accessTokenExpiresOn, connectionString)
        {
            DataFactory = dataFactory;
        }

        private async Task<DataFactoryManagementClient> GetClientAsync()
        {
            var credentials = await CheckAccessTokenValidityAsync(ResourceUrl);
            return new DataFactoryManagementClient(credentials) { SubscriptionId = DataFactory.SubscriptionId };
        }

        public async Task<string> StartPipelineRunAsync(string pipelineName, IDictionary<string, object> parameters, CancellationToken cancellationToken)
        {
            var client = await GetClientAsync();
            var createRunResponse = await client.Pipelines.CreateRunAsync(DataFactory.ResourceGroupName, DataFactory.ResourceName, pipelineName,
                parameters: parameters, cancellationToken: cancellationToken);
            return createRunResponse.RunId;
        }

        public async Task<PipelineRun> GetPipelineRunAsync(string runId, CancellationToken cancellationToken)
        {
            var client = await GetClientAsync();
            return await client.PipelineRuns.GetAsync(DataFactory.ResourceGroupName, DataFactory.ResourceName, runId, cancellationToken);
        }

        public async Task CancelPipelineRunAsync(string runId)
        {
            var client = await GetClientAsync();
            await client.PipelineRuns.CancelAsync(DataFactory.ResourceGroupName, DataFactory.ResourceName, runId, isRecursive: true);
        }

        public async Task<Dictionary<string, List<string>>> GetPipelinesAsync()
        {
            var client = await GetClientAsync();
            var pipelines = await client.Pipelines.ListByFactoryAsync(DataFactory.ResourceGroupName, DataFactory.ResourceName);
            // Key = Folder
            // Value = List of pipelines in that folder
            return pipelines
                .GroupBy(p => p.Folder?.Name ?? "/") // Replace null folder (root) with forward slash.
                .ToDictionary(p => p.Key, p => p.Select(p => p.Name).ToList());
        }

        public static async Task<DataFactoryHelper> GetDataFactoryHelperAsync(IDbContextFactory<EtlManagerContext> dbContextFactory, Guid dataFactoryId)
        {
            using var context = dbContextFactory.CreateDbContext();
            var dataFactory = await context.DataFactories
                .Include(df => df.AppRegistration)
                .FirstAsync(df => df.DataFactoryId == dataFactoryId);
            var connectionString = context.Database.GetConnectionString();
            (var accessToken, var accessTokenExpiresOn) = await GetAccessTokenAsync(dataFactory.AppRegistration, connectionString);

            return new DataFactoryHelper(
                dataFactory: dataFactory,
                accessToken: accessToken,
                accessTokenExpiresOn: accessTokenExpiresOn,
                connectionString: connectionString
            );
        }

        public static async Task TestConnection(AppRegistration appRegistration, string subscriptionId, string resourceGroupName, string resourceName)
        {
            var context = new AuthenticationContext(AuthenticationUrl + appRegistration.TenantId);
            var clientCredential = new ClientCredential(appRegistration.ClientId, appRegistration.ClientSecret);
            var result = await context.AcquireTokenAsync(ResourceUrl, clientCredential);
            var credentials = new TokenCredentials(result.AccessToken);
            var client = new DataFactoryManagementClient(credentials) { SubscriptionId = subscriptionId };
            var _ = await client.Factories.GetAsync(resourceGroupName, resourceName);
        }
    }
}
