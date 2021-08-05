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
    public class DataFactoryHelper
    {
        public DataFactory DataFactory { get; init; }
        private string? AccessToken { get; set; }
        private DateTime? AccessTokenExpiresOn { get; set; }
        private string ConnectionString { get; init; }

        private const string AuthenticationUrl = "https://login.microsoftonline.com/";
        private const string ResourceUrl = "https://management.azure.com/";

        private DataFactoryHelper(
            DataFactory dataFactory,
            string? accessToken,
            DateTime? accessTokenExpiresOn,
            string connectionString
            )
        {
            DataFactory = dataFactory;
            AccessToken = accessToken;
            AccessTokenExpiresOn = accessTokenExpiresOn;
            ConnectionString = connectionString;
        }

        public async Task<string> StartPipelineRunAsync(string pipelineName, IDictionary<string, object> parameters, CancellationToken cancellationToken)
        {
            var client = await CheckAccessTokenValidityAsync();
            var createRunResponse = await client.Pipelines.CreateRunAsync(DataFactory.ResourceGroupName, DataFactory.ResourceName, pipelineName,
                parameters: parameters, cancellationToken: cancellationToken);
            return createRunResponse.RunId;
        }

        public async Task<PipelineRun> GetPipelineRunAsync(string runId, CancellationToken cancellationToken)
        {
            var client = await CheckAccessTokenValidityAsync();
            return await client.PipelineRuns.GetAsync(DataFactory.ResourceGroupName, DataFactory.ResourceName, runId, cancellationToken);
        }

        public async Task CancelPipelineRunAsync(string runId)
        {
            var client = await CheckAccessTokenValidityAsync();
            await client.PipelineRuns.CancelAsync(DataFactory.ResourceGroupName, DataFactory.ResourceName, runId, isRecursive: true);
        }

        public async Task<Dictionary<string, List<string>>> GetPipelinesAsync()
        {
            var client = await CheckAccessTokenValidityAsync();
            var pipelines = await client.Pipelines.ListByFactoryAsync(DataFactory.ResourceGroupName, DataFactory.ResourceName);
            // Key = Folder
            // Value = List of pipelines in that folder
            return pipelines
                .GroupBy(p => p.Folder?.Name ?? "/") // Replace null folder (root) with forward slash.
                .ToDictionary(p => p.Key, p => p.Select(p => p.Name).ToList());
        }

        private async Task<DataFactoryManagementClient> CheckAccessTokenValidityAsync()
        {
            DataFactoryManagementClient? client = null;
            if (AccessTokenExpiresOn is null || DateTime.Now >= AccessTokenExpiresOn?.AddMinutes(-5)) // five minute safety margin
            {
                try
                {
                    var context = new AuthenticationContext(AuthenticationUrl + DataFactory.AppRegistration.TenantId);
                    var clientCredential = new ClientCredential(DataFactory.AppRegistration.ClientId, DataFactory.AppRegistration.ClientSecret);
                    var result = await context.AcquireTokenAsync(ResourceUrl, clientCredential);
                    AccessToken = result.AccessToken;
                    AccessTokenExpiresOn = result.ExpiresOn.ToLocalTime().DateTime;
                }
                catch (Exception ex)
                {
                    Log.Error(ex, "Error getting Microsoft OAuth access token for Data Factory id {DataFactoryId}", DataFactory.DataFactoryId);
                    throw;
                }

                var credentials = new TokenCredentials(AccessToken);
                client = new DataFactoryManagementClient(credentials) { SubscriptionId = DataFactory.SubscriptionId };

                // Update the token and its expiration time to the database for later use.
                try
                {
                    using var sqlConnection = new SqlConnection(ConnectionString);
                    await sqlConnection.ExecuteAsync(
                        @"UPDATE etlmanager.AppRegistration
                        SET AccessToken = @AccessToken, AccessTokenExpiresOn = @AccessTokenExpiresOn
                        WHERE AppRegistrationId = @AppRegistrationId",
                        new { AccessToken, AccessTokenExpiresOn, DataFactory.AppRegistration.AppRegistrationId });
                }
                catch (Exception ex)
                {
                    Log.Error(ex, "Error updating the OAuth access token for Data Factory id {DataFactoryId}", DataFactory.DataFactoryId);
                    throw;
                }
            }
            else if (client is null)
            {
                var credentials = new TokenCredentials(AccessToken);
                client = new DataFactoryManagementClient(credentials) { SubscriptionId = DataFactory.SubscriptionId };
            }
            return client;
        }

        public static async Task<DataFactoryHelper> GetDataFactoryHelperAsync(IDbContextFactory<EtlManagerContext> dbContextFactory, Guid dataFactoryId)
        {
            using var context = dbContextFactory.CreateDbContext();
            var dataFactory = await context.DataFactories
                .Include(df => df.AppRegistration)
                .FirstAsync(df => df.DataFactoryId == dataFactoryId);
            var connectionString = context.Database.GetConnectionString();
            using var sqlConnection = new SqlConnection(connectionString);
            (var accessToken, var accessTokenExpiresOn) = await sqlConnection.QueryFirstAsync<(string?, DateTime?)>(
                    @"SELECT [AccessToken], [AccessTokenExpiresOn]
                    FROM etlmanager.AppRegistration
                    WHERE AppRegistrationId = @AppRegistrationId",
                    new { dataFactory.AppRegistration.AppRegistrationId });

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
