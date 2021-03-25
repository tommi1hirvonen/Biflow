using Microsoft.IdentityModel.Clients.ActiveDirectory;
using Serilog;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EtlManagerExecutor
{
    class PowerBIService
    {
        public string PowerBIServiceId { get; init; }
        public string TenantId { get; init; }
        public string ClientId { get; init; }
        public string ClientSecret { get; init; }
        public string AccessToken { get; set; }
        public DateTime? AccessTokenExpiresOn { get; set; }

        public async Task<bool> CheckAccessTokenValidityAsync(string connectionString)
        {
            if (AccessTokenExpiresOn is null || DateTime.Now >= AccessTokenExpiresOn?.AddMinutes(-5)) // five minute safety margin
            {
                try
                {
                    var context = new AuthenticationContext("https://login.microsoftonline.com/" + TenantId);
                    var clientCredential = new ClientCredential(ClientId, ClientSecret);
                    var result = await context.AcquireTokenAsync("https://analysis.windows.net/powerbi/api", clientCredential);
                    AccessToken = result.AccessToken;
                    AccessTokenExpiresOn = result.ExpiresOn.ToLocalTime().DateTime;
                }
                catch (Exception ex)
                {
                    Log.Error(ex, "Error getting Microsoft OAuth access token for Power BI Service id {PowerBIServiceId}", PowerBIServiceId);
                    throw;
                }

                // Update the token and its expiration time to the database for later use.
                try
                {
                    using var sqlConnection = new SqlConnection(connectionString);
                    await sqlConnection.OpenAsync();
                    using var updateTokenCmd = new SqlCommand(
                        @"UPDATE etlmanager.PowerBIService
                    SET AccessToken = @AccessToken, AccessTokenExpiresOn = @AccessTokenExpiresOn
                    WHERE PowerBIServiceId = @PowerBIServiceId", sqlConnection);
                    updateTokenCmd.Parameters.AddWithValue("@AccessToken", AccessToken);
                    updateTokenCmd.Parameters.AddWithValue("@AccessTokenExpiresOn", AccessTokenExpiresOn);
                    updateTokenCmd.Parameters.AddWithValue("@PowerBIServiceId", PowerBIServiceId);
                    await updateTokenCmd.ExecuteNonQueryAsync();
                }
                catch (Exception ex)
                {
                    Log.Error(ex, "Error updating the OAuth access token for Power BI Service id {PowerBIServiceId}", PowerBIServiceId);
                    throw;
                }
                return false;
            }
            else
            {
                return true;
            }
        }

        public static async Task<PowerBIService> GetPowerBIServiceAsync(string connectionString, string powerBIServiceId, string encryptionKey)
        {
            using var sqlConnection = new SqlConnection(connectionString);
            await sqlConnection.OpenAsync();
            using var sqlCommand = new SqlCommand(
                @"SELECT [TenantId], [ClientId], etlmanager.GetDecryptedValue(@EncryptionKey, ClientSecret) AS ClientSecret,
                        [AccessToken], [AccessTokenExpiresOn]
                FROM etlmanager.PowerBIService
                WHERE PowerBIServiceId = @PowerBIServiceId", sqlConnection);
            sqlCommand.Parameters.AddWithValue("@PowerBIServiceId", powerBIServiceId);
            sqlCommand.Parameters.AddWithValue("@EncryptionKey", encryptionKey);

            using var reader = await sqlCommand.ExecuteReaderAsync();
            await reader.ReadAsync();

            string tenantId = reader["TenantId"].ToString();
            string clientId = reader["ClientId"].ToString();
            string clientSecret = reader["ClientSecret"].ToString();
            string accessToken = null;
            DateTime? accessTokenExpiresOn = null;
            if (reader["AccessToken"] != DBNull.Value) accessToken = reader["AccessToken"].ToString();
            if (reader["AccessTokenExpiresOn"] != DBNull.Value) accessTokenExpiresOn = (DateTime)reader["AccessTokenExpiresOn"];

            return new PowerBIService
            {
                PowerBIServiceId = powerBIServiceId,
                TenantId = tenantId,
                ClientId = clientId,
                ClientSecret = clientSecret,
                AccessToken = accessToken,
                AccessTokenExpiresOn = accessTokenExpiresOn
            };
        }
    }
}
