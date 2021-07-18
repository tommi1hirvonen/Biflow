using Dapper;
using EtlManagerDataAccess.Models;
using Microsoft.EntityFrameworkCore;
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

namespace EtlManagerDataAccess
{
    public class PowerBIServiceHelper
    {
        public PowerBIService PowerBIService { get; init; }
        private string? AccessToken { get; set; }
        private DateTime? AccessTokenExpiresOn { get; set; }
        private string ConnectionString { get; init; }

        private const string AuthenticationUrl = "https://login.microsoftonline.com/";
        private const string ResourceUrl = "https://analysis.windows.net/powerbi/api";

        private PowerBIServiceHelper(
            PowerBIService powerBIService,
            string? accessToken,
            DateTime? accessTokenExpiresOn,
            string connectionString
            )
        {
            PowerBIService = powerBIService;
            AccessToken = accessToken;
            AccessTokenExpiresOn = accessTokenExpiresOn;
            ConnectionString = connectionString;
        }

        public async Task RefreshDatasetAsync(string groupId, string datasetId, CancellationToken cancellationToken)
        {
            var client = await CheckAccessTokenValidityAsync();
            await client.Datasets.RefreshDatasetInGroupAsync(Guid.Parse(groupId), datasetId, cancellationToken: cancellationToken);
        }

        public async Task<Refresh?> GetDatasetRefreshStatus(string groupId, string datasetId, CancellationToken cancellationToken)
        {
            var client = await CheckAccessTokenValidityAsync();
            var refresh = await client.Datasets.GetRefreshHistoryInGroupAsync(Guid.Parse(groupId), datasetId, top: 1, cancellationToken);
            return refresh.Value.FirstOrDefault();
        }

        public async Task<Dictionary<(string GroupId, string GroupName), List<(string DatasetId, string DatasetName)>>> GetAllDatasetsAsync()
        {
            var client = await CheckAccessTokenValidityAsync();
            var groups = await client.Groups.GetGroupsAsync();
            var datasets = new Dictionary<(string GroupId, string GroupName), List<(string DatasetId, string DatasetName)>>();
            foreach (var group in groups.Value)
            {
                var groupDatasets = await client.Datasets.GetDatasetsInGroupAsync(group.Id);
                var key = (group.Id.ToString(), group.Name);
                datasets[key] = groupDatasets.Value.Select(d => (d.Id.ToString(), d.Name)).ToList();
            }
            return datasets;
        }

        private async Task<PowerBIClient> CheckAccessTokenValidityAsync()
        {
            PowerBIClient? client = null;
            if (AccessTokenExpiresOn is null || DateTime.Now >= AccessTokenExpiresOn?.AddMinutes(-5)) // five minute safety margin
            {
                try
                {
                    var context = new AuthenticationContext(AuthenticationUrl + PowerBIService.TenantId);
                    var clientCredential = new ClientCredential(PowerBIService.ClientId, PowerBIService.ClientSecret);
                    var result = await context.AcquireTokenAsync(ResourceUrl, clientCredential);
                    AccessToken = result.AccessToken;
                    AccessTokenExpiresOn = result.ExpiresOn.ToLocalTime().DateTime;
                }
                catch (Exception ex)
                {
                    Log.Error(ex, "Error getting Microsoft OAuth access token for Power BI Service id {PowerBIServiceId}", PowerBIService.PowerBIServiceId);
                    throw;
                }

                var credentials = new TokenCredentials(AccessToken);
                client = new PowerBIClient(credentials);

                // Update the token and its expiration time to the database for later use.
                try
                {
                    using var sqlConnection = new SqlConnection(ConnectionString);
                    await sqlConnection.ExecuteAsync(
                        @"UPDATE etlmanager.PowerBIService
                        SET AccessToken = @AccessToken, AccessTokenExpiresOn = @AccessTokenExpiresOn
                        WHERE PowerBIServiceId = @PowerBIServiceId",
                        new { AccessToken, AccessTokenExpiresOn, PowerBIService.PowerBIServiceId });
                }
                catch (Exception ex)
                {
                    Log.Error(ex, "Error updating the OAuth access token for Power BI Service id {PowerBIServiceId}", PowerBIService.PowerBIServiceId);
                    throw;
                }
            }
            else if (client is null)
            {
                var credentials = new TokenCredentials(AccessToken);
                client = new PowerBIClient(credentials);
            }
            return client;
        }

        public static async Task<PowerBIServiceHelper> GetPowerBIServiceHelperAsync(IDbContextFactory<EtlManagerContext> dbContextFactory, Guid powerBIServiceId)
        {
            using var context = dbContextFactory.CreateDbContext();
            var connectionString = context.Database.GetConnectionString();
            var powerBIService = await context.PowerBIServices.FindAsync(powerBIServiceId);
            using var sqlConnection = new SqlConnection(context.Database.GetConnectionString());
            (var accessToken, var accessTokenExpiresOn) = await sqlConnection.QueryFirstAsync<(string?, DateTime?)>(
                    @"SELECT [AccessToken], [AccessTokenExpiresOn]
                    FROM etlmanager.PowerBIService
                    WHERE PowerBIServiceId = @PowerBIServiceId",
                    new { PowerBIServiceId = powerBIServiceId });

            return new PowerBIServiceHelper(
                powerBIService: powerBIService,
                accessToken: accessToken,
                accessTokenExpiresOn: accessTokenExpiresOn,
                connectionString: connectionString
            );
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
