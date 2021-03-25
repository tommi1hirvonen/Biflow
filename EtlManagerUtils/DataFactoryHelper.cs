using Microsoft.Azure.Management.DataFactory;
using Microsoft.Azure.Management.DataFactory.Models;
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
        private string AccessToken { get; set; }
        private DateTime? AccessTokenExpiresOn { get; set; }
        private string ConnectionString { get; init; }
        private DataFactoryManagementClient Client {get;set;}

        public async Task<string> StartPipelineRunAsync(string pipelineName, IDictionary<string, object> parameters, CancellationToken cancellationToken)
        {
            await CheckAccessTokenValidityAsync();
            var createRunResponse = await Client.Pipelines.CreateRunAsync(ResourceGroupName, ResourceName, pipelineName,
                parameters: parameters, cancellationToken: cancellationToken);
            return createRunResponse.RunId;
        }

        public async Task<PipelineRun> GetPipelineRunAsync(string runId, CancellationToken cancellationToken)
        {
            await CheckAccessTokenValidityAsync();
            return await Client.PipelineRuns.GetAsync(ResourceGroupName, ResourceName, runId, cancellationToken);
        }

        public async Task CancelPipelineRunAsync(string runId)
        {
            await CheckAccessTokenValidityAsync();
            await Client.PipelineRuns.CancelAsync(ResourceGroupName, ResourceName, runId, isRecursive: true);
        }

        private async Task CheckAccessTokenValidityAsync()
        {
            if (AccessTokenExpiresOn is null || DateTime.Now >= AccessTokenExpiresOn?.AddMinutes(-5)) // five minute safety margin
            {
                try
                {
                    var context = new AuthenticationContext("https://login.microsoftonline.com/" + TenantId);
                    var clientCredential = new ClientCredential(ClientId, ClientSecret);
                    var result = await context.AcquireTokenAsync("https://management.azure.com/", clientCredential);
                    AccessToken = result.AccessToken;
                    AccessTokenExpiresOn = result.ExpiresOn.ToLocalTime().DateTime;
                }
                catch (Exception ex)
                {
                    Log.Error(ex, "Error getting Microsoft OAuth access token for Data Factory id {DataFactoryId}", DataFactoryId);
                    throw;
                }

                var credentials = new TokenCredentials(AccessToken);
                Client = new DataFactoryManagementClient(credentials) { SubscriptionId = SubscriptionId };

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
            else if (Client is null)
            {
                var credentials = new TokenCredentials(AccessToken);
                Client = new DataFactoryManagementClient(credentials) { SubscriptionId = SubscriptionId };
            }
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

            string tenantId = reader["TenantId"].ToString();
            string subscriptionId = reader["SubscriptionId"].ToString();
            string resourceGroupName = reader["ResourceGroupName"].ToString();
            string resourceName = reader["ResourceName"].ToString();
            string clientId = reader["ClientId"].ToString();
            string clientSecret = reader["ClientSecret"].ToString();
            string accessToken = null;
            DateTime? accessTokenExpiresOn = null;
            if (reader["AccessToken"] != DBNull.Value) accessToken = reader["AccessToken"].ToString();
            if (reader["AccessTokenExpiresOn"] != DBNull.Value) accessTokenExpiresOn = (DateTime)reader["AccessTokenExpiresOn"];

            return new DataFactoryHelper
            {
                DataFactoryId = dataFactoryId,
                TenantId = tenantId,
                SubscriptionId = subscriptionId,
                ResourceGroupName = resourceGroupName,
                ResourceName = resourceName,
                ClientId = clientId,
                ClientSecret = clientSecret,
                AccessToken = accessToken,
                AccessTokenExpiresOn = accessTokenExpiresOn,
                ConnectionString = connectionString
            };
        }
    }
}
