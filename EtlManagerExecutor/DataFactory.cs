using Microsoft.IdentityModel.Clients.ActiveDirectory;
using Serilog;
using System;
using System.Data.SqlClient;
using System.Threading.Tasks;

namespace EtlManagerExecutor
{
    public class DataFactory
    {
        public string DataFactoryId { get; set; }
        public string TenantId { get; set; }
        public string SubscriptionId { get; set; }
        public string ResourceGroupName { get; set; }
        public string ResourceName { get; set; }
        public string ClientId { get; set; }
        public string ClientSecret { get; set; }
        public string AccessToken { get; set; }
        public DateTime? AccessTokenExpiresOn { get; set; }

        public async Task<bool> CheckAccessTokenValidityAsync(string connectionString)
        {
            if (AccessTokenExpiresOn == null || DateTime.Now >= AccessTokenExpiresOn?.AddMinutes(-5)) // five minute safety margin
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

                // Update the token and its expiration time to the database for later use.
                try
                {
                    using var sqlConnection = new SqlConnection(connectionString);
                    await sqlConnection.OpenAsync();
                    var updateTokenCmd = new SqlCommand(
                        @"UPDATE etlmanager.DataFactory
                    SET AccessToken = @AccessToken, AccessTokenExpiresOn = @AccessTokenExpiresOn
                    WHERE ClientId = @ClientId AND ClientSecret = @ClientSecret", sqlConnection);
                    updateTokenCmd.Parameters.AddWithValue("@AccessToken", AccessToken);
                    updateTokenCmd.Parameters.AddWithValue("@AccessTokenExpiresOn", AccessTokenExpiresOn);
                    updateTokenCmd.Parameters.AddWithValue("@ClientId", ClientId);
                    updateTokenCmd.Parameters.AddWithValue("@ClientSecret", ClientSecret);
                    await updateTokenCmd.ExecuteNonQueryAsync();
                }
                catch (Exception ex)
                {
                    Log.Error(ex, "Error updating the OAuth access token for Data Factory id {DataFactoryId}", DataFactoryId);
                    throw;
                }
                return false;
            }
            else
            {
                return true;
            }
        }

        public static async Task<DataFactory> GetDataFactoryAsync(string connectionString, string dataFactoryId, string encryptionKey)
        {
            using var sqlConnection = new SqlConnection(connectionString);
            await sqlConnection.OpenAsync();
            var sqlCommand = new SqlCommand(
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

            return new DataFactory
            {
                DataFactoryId = dataFactoryId,
                TenantId = tenantId,
                SubscriptionId = subscriptionId,
                ResourceGroupName = resourceGroupName,
                ResourceName = resourceName,
                ClientId = clientId,
                ClientSecret = clientSecret,
                AccessToken = accessToken,
                AccessTokenExpiresOn = accessTokenExpiresOn
            };
        }
    }
}
