using Dapper;
using EtlManagerDataAccess.Models;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Clients.ActiveDirectory;
using Microsoft.PowerBI.Api;
using Microsoft.PowerBI.Api.Models;
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
    public class PowerBIServiceHelper : AzureHelperBase
    {
       
        private const string ResourceUrl = "https://analysis.windows.net/powerbi/api";

        private PowerBIServiceHelper(AppRegistration appRegistration, string connectionString) : base(appRegistration, connectionString)
        {
        }

        private async Task<PowerBIClient> GetClientAsync()
        {
            var accessToken = await CheckAccessTokenValidityAsync(ResourceUrl);
            var credentials = new TokenCredentials(accessToken);
            return new PowerBIClient(credentials);
        }

        public async Task RefreshDatasetAsync(string groupId, string datasetId, CancellationToken cancellationToken)
        {
            var client = await GetClientAsync();
            await client.Datasets.RefreshDatasetInGroupAsync(Guid.Parse(groupId), datasetId, cancellationToken: cancellationToken);
        }

        public async Task<Refresh?> GetDatasetRefreshStatus(string groupId, string datasetId, CancellationToken cancellationToken)
        {
            var client = await GetClientAsync();
            var refresh = await client.Datasets.GetRefreshHistoryInGroupAsync(Guid.Parse(groupId), datasetId, top: 1, cancellationToken);
            return refresh.Value.FirstOrDefault();
        }

        public async Task<Dictionary<(string GroupId, string GroupName), List<(string DatasetId, string DatasetName)>>> GetAllDatasetsAsync()
        {
            var client = await GetClientAsync();
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

        public static async Task<PowerBIServiceHelper> GetPowerBIServiceHelperAsync(IDbContextFactory<EtlManagerContext> dbContextFactory, Guid appRegistrationId)
        {
            using var context = dbContextFactory.CreateDbContext();
            var connectionString = context.Database.GetConnectionString();
            var appRegistration = await context.AppRegistrations.FindAsync(appRegistrationId);

            return new PowerBIServiceHelper(appRegistration, connectionString);
        }

        public static new async Task TestConnection(AppRegistration appRegistration)
        {
            var context = new AuthenticationContext(AuthenticationUrl + appRegistration.TenantId);
            var clientCredential = new ClientCredential(appRegistration.ClientId, appRegistration.ClientSecret);
            var result = await context.AcquireTokenAsync(ResourceUrl, clientCredential);
            var credentials = new TokenCredentials(result.AccessToken);
            var client = new PowerBIClient(credentials);
            var _ = await client.Groups.GetGroupsAsync();
        }
    }
}
