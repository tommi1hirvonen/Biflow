using Dapper;
using EtlManagerDataAccess.Models;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Clients.ActiveDirectory;
using Microsoft.Rest;
using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace EtlManagerDataAccess
{
    public class FunctionAppHelper : AzureHelperBase
    {
        public FunctionApp FunctionApp { get; init; }
        
        private const string ResourceUrl = "https://management.azure.com/";

        private FunctionAppHelper(FunctionApp functionApp, string connectionString) : base(functionApp.AppRegistration, connectionString)
        {
            FunctionApp = functionApp;
        }
        
        public async Task<List<(string FunctionName, string FunctionType, string FunctionUrl)>> GetFunctionsAsync()
        {
            var accessToken = await CheckAccessTokenValidityAsync(ResourceUrl);
            var functionListUrl = $"https://management.azure.com/subscriptions/{FunctionApp.SubscriptionId}/resourceGroups/{FunctionApp.ResourceGroupName}/providers/Microsoft.Web/sites/{FunctionApp.ResourceName}/functions?api-version=2015-08-01";
            var message = new HttpRequestMessage(HttpMethod.Get, functionListUrl);
            message.Headers.Add("authorization", $"Bearer {accessToken}");
            var client = new HttpClient();
            var response = await client.SendAsync(message);
            var content = await response.Content.ReadAsStringAsync();

            var json = JsonSerializer.Deserialize<JsonElement>(content);
            var value = json.GetProperty("value");
            var functionArray = value.EnumerateArray();
            var functions = functionArray.Select(func =>
            {
                var properties = func.GetProperty("properties");
                var name = properties.GetProperty("name").GetString() ?? "";
                var url = properties.GetProperty("invoke_url_template").GetString() ?? "";
                var config = properties.GetProperty("config");
                var bindings = config.GetProperty("bindings").EnumerateArray();
                string type = "";
                if (bindings.MoveNext())
                {
                    type = bindings.Current.GetProperty("type").GetString() ?? "";
                }
                return (name, type, url);
            }).ToList();

            return functions;
        }

        public static async Task<FunctionAppHelper> GetFunctionAppHelperAsync(IDbContextFactory<EtlManagerContext> dbContextFactory, Guid functionAppId)
        {
            using var context = dbContextFactory.CreateDbContext();
            var functionApp = await context.FunctionApps
                .Include(fa => fa.AppRegistration)
                .FirstAsync(fa => fa.FunctionAppId == functionAppId);
            var connectionString = context.Database.GetConnectionString();

            return new FunctionAppHelper(functionApp, connectionString);
        }

        public static async Task TestConnection(AppRegistration appRegistration, string subscriptionId, string resourceGroupName, string resourceName)
        {
            var context = new AuthenticationContext(AuthenticationUrl + appRegistration.TenantId);
            var clientCredential = new ClientCredential(appRegistration.ClientId, appRegistration.ClientSecret);
            var result = await context.AcquireTokenAsync(ResourceUrl, clientCredential);

            var functionListUrl = $"https://management.azure.com/subscriptions/{subscriptionId}/resourceGroups/{resourceGroupName}/providers/Microsoft.Web/sites/{resourceName}/functions?api-version=2015-08-01";
            var message = new HttpRequestMessage(HttpMethod.Get, functionListUrl);
            message.Headers.Add("authorization", $"Bearer {result.AccessToken}");
            var client = new HttpClient();
            var response = await client.SendAsync(message);
            response.EnsureSuccessStatusCode();
        }
    }
}
