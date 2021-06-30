using Microsoft.Azure.Management.DataFactory;
using Microsoft.Azure.Management.DataFactory.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Clients.ActiveDirectory;
using Microsoft.Rest;
using Serilog;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace EtlManagerUtils
{
    public class DataFactoryHelper
    {
        public string DataFactoryId { get; init; }
        private string TenantId { get; init; }
        private string SubscriptionId { get; init; }
        private string ResourceGroupName { get; init; }
        private string ResourceName { get; init; }
        private string ClientId { get; init; }
        private string ClientSecret { get; init; }
        private string? AccessToken { get; set; }
        private DateTime? AccessTokenExpiresOn { get; set; }
        private string ConnectionString { get; init; }

        private const string AuthenticationUrl = "https://login.microsoftonline.com/";
        private const string ResourceUrl = "https://management.azure.com/";

        private DataFactoryHelper(
            string dataFactoryId,
            string tenantId,
            string subscriptionId,
            string resourceGroupName,
            string resourceName,
            string clientId,
            string clientSecret,
            string? accessToken,
            DateTime? accessTokenExpiresOn,
            string connectionString
            )
        {
            DataFactoryId = dataFactoryId;
            TenantId = tenantId;
            SubscriptionId = subscriptionId;
            ResourceGroupName = resourceGroupName;
            ResourceName = resourceName;
            ClientId = clientId;
            ClientSecret = clientSecret;
            AccessToken = accessToken;
            AccessTokenExpiresOn = accessTokenExpiresOn;
            ConnectionString = connectionString;
        }

        public async Task<string> StartPipelineRunAsync(string pipelineName, IDictionary<string, object> parameters, CancellationToken cancellationToken)
        {
            var client = await CheckAccessTokenValidityAsync();
            var createRunResponse = await client.Pipelines.CreateRunAsync(ResourceGroupName, ResourceName, pipelineName,
                parameters: parameters, cancellationToken: cancellationToken);
            return createRunResponse.RunId;
        }

        public async Task<PipelineRun> GetPipelineRunAsync(string runId, CancellationToken cancellationToken)
        {
            var client = await CheckAccessTokenValidityAsync();
            return await client.PipelineRuns.GetAsync(ResourceGroupName, ResourceName, runId, cancellationToken);
        }

        public async Task CancelPipelineRunAsync(string runId)
        {
            var client = await CheckAccessTokenValidityAsync();
            await client.PipelineRuns.CancelAsync(ResourceGroupName, ResourceName, runId, isRecursive: true);
        }

        public async Task<Dictionary<string, List<string>>> GetPipelinesAsync()
        {
            var client = await CheckAccessTokenValidityAsync();
            var pipelines = await client.Pipelines.ListByFactoryAsync(ResourceGroupName, ResourceName);
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
                    var context = new AuthenticationContext(AuthenticationUrl + TenantId);
                    var clientCredential = new ClientCredential(ClientId, ClientSecret);
                    var result = await context.AcquireTokenAsync(ResourceUrl, clientCredential);
                    AccessToken = result.AccessToken;
                    AccessTokenExpiresOn = result.ExpiresOn.ToLocalTime().DateTime;
                }
                catch (Exception ex)
                {
                    Log.Error(ex, "Error getting Microsoft OAuth access token for Data Factory id {DataFactoryId}", DataFactoryId);
                    throw;
                }

                var credentials = new TokenCredentials(AccessToken);
                client = new DataFactoryManagementClient(credentials) { SubscriptionId = SubscriptionId };

                // Update the token and its expiration time to the database for later use.
                try
                {
                    using var sqlConnection = new SqlConnection(ConnectionString);
                    await sqlConnection.OpenAsync();
                    using var updateTokenCmd = new SqlCommand(
                        @"UPDATE etlmanager.DataFactory
                        SET AccessToken = @AccessToken, AccessTokenExpiresOn = @AccessTokenExpiresOn
                        WHERE DataFactoryId = @DataFactoryId", sqlConnection);
                    updateTokenCmd.Parameters.AddWithValue("@AccessToken", AccessToken);
                    updateTokenCmd.Parameters.AddWithValue("@AccessTokenExpiresOn", AccessTokenExpiresOn);
                    updateTokenCmd.Parameters.AddWithValue("@DataFactoryId", DataFactoryId);
                    await updateTokenCmd.ExecuteNonQueryAsync();
                }
                catch (Exception ex)
                {
                    Log.Error(ex, "Error updating the OAuth access token for Data Factory id {DataFactoryId}", DataFactoryId);
                    throw;
                }
            }
            else if (client is null)
            {
                var credentials = new TokenCredentials(AccessToken);
                client = new DataFactoryManagementClient(credentials) { SubscriptionId = SubscriptionId };
            }
            return client;
        }

        public static async Task<DataFactoryHelper> GetDataFactoryHelperAsync(IConfiguration configuration, string dataFactoryId)
        {
            var connectionString = configuration.GetConnectionString("EtlManagerContext");
            var encryptionKey = await CommonUtility.GetEncryptionKeyAsync(configuration)
                ?? throw new ArgumentNullException("encyptionKey", "Encryption key cannot be null in order to create a DataFactoryHelper");
            return await GetDataFactoryHelperAsync(connectionString, dataFactoryId, encryptionKey);
        }

        public static async Task<DataFactoryHelper> GetDataFactoryHelperAsync(string connectionString, string dataFactoryId, string encryptionKey)
        {
            using var sqlConnection = new SqlConnection(connectionString);
            await sqlConnection.OpenAsync();
            using var sqlCommand = new SqlCommand(
                @"SELECT [TenantId], [SubscriptionId], [ClientId], etlmanager.GetDecryptedValue(@EncryptionKey, ClientSecret) AS ClientSecret,
                        [ResourceGroupName], [ResourceName], [AccessToken], [AccessTokenExpiresOn]
                FROM etlmanager.DataFactory
                WHERE DataFactoryId = @DataFactoryId", sqlConnection);
            sqlCommand.Parameters.AddWithValue("@DataFactoryId", dataFactoryId);
            sqlCommand.Parameters.AddWithValue("@EncryptionKey", encryptionKey);

            using var reader = await sqlCommand.ExecuteReaderAsync();
            await reader.ReadAsync();

            string tenantId = reader["TenantId"].ToString() ?? throw new ArgumentNullException(nameof(tenantId), "TenantId was null");
            string subscriptionId = reader["SubscriptionId"].ToString() ?? throw new ArgumentNullException(nameof(subscriptionId), "SubscriptionId was null");
            string resourceGroupName = reader["ResourceGroupName"].ToString() ?? throw new ArgumentNullException(nameof(resourceGroupName), "ResourceGroupName was null");
            string resourceName = reader["ResourceName"].ToString() ?? throw new ArgumentNullException(nameof(resourceName), "ResourceName was null");
            string clientId = reader["ClientId"].ToString() ?? throw new ArgumentNullException(nameof(clientId), "ClientId was null");
            string clientSecret = reader["ClientSecret"].ToString() ?? throw new ArgumentNullException(nameof(clientSecret), "ClientSecret was null");
            string? accessToken = null;
            DateTime? accessTokenExpiresOn = null;
            if (reader["AccessToken"] != DBNull.Value) accessToken = reader["AccessToken"].ToString();
            if (reader["AccessTokenExpiresOn"] != DBNull.Value) accessTokenExpiresOn = (DateTime)reader["AccessTokenExpiresOn"];

            return new DataFactoryHelper(
                dataFactoryId: dataFactoryId,
                tenantId: tenantId,
                subscriptionId: subscriptionId,
                resourceGroupName: resourceGroupName,
                resourceName: resourceName,
                clientId: clientId,
                clientSecret: clientSecret,
                accessToken: accessToken,
                accessTokenExpiresOn: accessTokenExpiresOn,
                connectionString: connectionString
            );
        }

        public static async Task TestConnection(string tenantId, string clientId, string clientSecret, string subscriptionId, string resourceGroupName, string resourceName)
        {
            var context = new AuthenticationContext(AuthenticationUrl + tenantId);
            var clientCredential = new ClientCredential(clientId, clientSecret);
            var result = await context.AcquireTokenAsync(ResourceUrl, clientCredential);
            var credentials = new TokenCredentials(result.AccessToken);
            var client = new DataFactoryManagementClient(credentials) { SubscriptionId = subscriptionId };
            var _ = await client.Factories.GetAsync(resourceGroupName, resourceName);
        }
    }
}
