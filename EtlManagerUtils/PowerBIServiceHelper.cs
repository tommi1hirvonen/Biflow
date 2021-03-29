using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Clients.ActiveDirectory;
using Microsoft.PowerBI.Api;
using Microsoft.PowerBI.Api.Models;
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
    public class PowerBIServiceHelper
    {
        public string PowerBIServiceId { get; init; }
        private string TenantId { get; init; }
        private string ClientId { get; init; }
        private string ClientSecret { get; init; }
        private string AccessToken { get; set; }
        private DateTime? AccessTokenExpiresOn { get; set; }
        private string ConnectionString { get; init; }
        private PowerBIClient Client { get; set; }

        private const string AuthenticationUrl = "https://login.microsoftonline.com/";
        private const string ResourceUrl = "https://analysis.windows.net/powerbi/api";

        public async Task RefreshDatasetAsync(string groupId, string datasetId, CancellationToken cancellationToken)
        {
            await CheckAccessTokenValidityAsync();
            await Client.Datasets.RefreshDatasetInGroupAsync(Guid.Parse(groupId), datasetId, cancellationToken: cancellationToken);
        }

        public async Task<Refresh> GetDatasetRefreshStatus(string groupId, string datasetId, CancellationToken cancellationToken)
        {
            await CheckAccessTokenValidityAsync();
            var refresh = await Client.Datasets.GetRefreshHistoryInGroupAsync(Guid.Parse(groupId), datasetId, top: 1, cancellationToken);
            return refresh.Value.FirstOrDefault();
        }

        public async Task<Dictionary<(string GroupId, string GroupName), List<(string DatasetId, string DatasetName)>>> GetAllDatasetsAsync()
        {
            await CheckAccessTokenValidityAsync();
            var groups = await Client.Groups.GetGroupsAsync();
            var datasets = new Dictionary<(string GroupId, string GroupName), List<(string DatasetId, string DatasetName)>>();
            foreach (var group in groups.Value)
            {
                var groupDatasets = await Client.Datasets.GetDatasetsInGroupAsync(group.Id);
                var key = (group.Id.ToString(), group.Name);
                datasets[key] = groupDatasets.Value.Select(d => (d.Id.ToString(), d.Name)).ToList();
            }
            return datasets;
        }

        private async Task CheckAccessTokenValidityAsync()
        {
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
                    Log.Error(ex, "Error getting Microsoft OAuth access token for Power BI Service id {PowerBIServiceId}", PowerBIServiceId);
                    throw;
                }

                var credentials = new TokenCredentials(AccessToken);
                Client = new PowerBIClient(credentials);

                // Update the token and its expiration time to the database for later use.
                try
                {
                    using var sqlConnection = new SqlConnection(ConnectionString);
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
            }
            else if (Client is null)
            {
                var credentials = new TokenCredentials(AccessToken);
                Client = new PowerBIClient(credentials);
            }
        }

        public static async Task<PowerBIServiceHelper> GetPowerBIServiceHelperAsync(IConfiguration configuration, string powerBIServiceId)
        {
            var connectionString = configuration.GetConnectionString("EtlManagerContext");
            var encryptionKey = await CommonUtility.GetEncryptionKeyAsync(configuration);
            return await GetPowerBIServiceHelperAsync(connectionString, powerBIServiceId, encryptionKey);
        }

        public static async Task<PowerBIServiceHelper> GetPowerBIServiceHelperAsync(string connectionString, string powerBIServiceId, string encryptionKey)
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

            return new PowerBIServiceHelper
            {
                PowerBIServiceId = powerBIServiceId,
                TenantId = tenantId,
                ClientId = clientId,
                ClientSecret = clientSecret,
                AccessToken = accessToken,
                AccessTokenExpiresOn = accessTokenExpiresOn,
                ConnectionString = connectionString
            };
        }

        public static async Task TestConnection(string tenantId, string clientId, string clientSecret)
        {
            var context = new AuthenticationContext(AuthenticationUrl + tenantId);
            var clientCredential = new ClientCredential(clientId, clientSecret);
            var result = await context.AcquireTokenAsync(ResourceUrl, clientCredential);
            var credentials = new TokenCredentials(result.AccessToken);
            var client = new PowerBIClient(credentials);
            var _ = await client.Groups.GetGroupsAsync();
        }
    }
}
