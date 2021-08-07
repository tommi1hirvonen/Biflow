using Dapper;
using EtlManagerDataAccess.Models;
using Microsoft.Data.SqlClient;
using Microsoft.IdentityModel.Clients.ActiveDirectory;
using Microsoft.Rest;
using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EtlManagerDataAccess
{
    public abstract class AzureHelperBase
    {
        protected AppRegistration AppRegistration { get; init; }
        private string? AccessToken { get; set; }
        private DateTime? AccessTokenExpiresOn { get; set; }
        private string ConnectionString { get; init; }

        protected const string AuthenticationUrl = "https://login.microsoftonline.com/";

        protected AzureHelperBase(
            AppRegistration appRegistration,
            string connectionString
            )
        {
            AppRegistration = appRegistration;
            ConnectionString = connectionString;
        }

        protected async Task<string> CheckAccessTokenValidityAsync(string resourceUrl)
        {
            // If the access token is not set, get it from the database first.
            if (AccessToken is null || AccessTokenExpiresOn is null)
            {
                using var sqlConnection = new SqlConnection(ConnectionString);
                (AccessToken, AccessTokenExpiresOn) = await sqlConnection.QueryFirstAsync<(string?, DateTime?)>(
                    @"SELECT [AccessToken], [AccessTokenExpiresOn]
                    FROM etlmanager.AppRegistration
                    WHERE AppRegistrationId = @AppRegistrationId",
                    new { AppRegistration.AppRegistrationId });
            }

            // If the access token was not set in database, get it from the API.
            if (AccessToken is null || AccessTokenExpiresOn is null || DateTime.Now >= AccessTokenExpiresOn?.AddMinutes(-5)) // five minute safety margin
            {
                try
                {
                    var context = new AuthenticationContext(AuthenticationUrl + AppRegistration.TenantId);
                    var clientCredential = new ClientCredential(AppRegistration.ClientId, AppRegistration.ClientSecret);
                    var result = await context.AcquireTokenAsync(resourceUrl, clientCredential);
                    AccessToken = result.AccessToken;
                    AccessTokenExpiresOn = result.ExpiresOn.ToLocalTime().DateTime;
                }
                catch (Exception ex)
                {
                    Log.Error(ex, "Error getting Microsoft OAuth access token for Power BI Service id {PowerBIServiceId}", AppRegistration.AppRegistrationId);
                    throw;
                }

                // Update the token and its expiration time to the database for later use.
                try
                {
                    using var sqlConnection = new SqlConnection(ConnectionString);
                    await sqlConnection.ExecuteAsync(
                        @"UPDATE etlmanager.AppRegistration
                        SET AccessToken = @AccessToken, AccessTokenExpiresOn = @AccessTokenExpiresOn
                        WHERE AppRegistrationId = @AppRegistrationId",
                        new { AccessToken, AccessTokenExpiresOn, AppRegistration.AppRegistrationId });
                }
                catch (Exception ex)
                {
                    Log.Error(ex, "Error updating the OAuth access token for Power BI Service id {PowerBIServiceId}", AppRegistration.AppRegistrationId);
                    throw;
                }
            }

            return AccessToken;
        }

        public static async Task TestConnection(AppRegistration appRegistration)
        {
            var context = new AuthenticationContext(AuthenticationUrl + appRegistration.TenantId);
            var clientCredential = new ClientCredential(appRegistration.ClientId, appRegistration.ClientSecret);
            var resourceUrl = "https://management.azure.com/";
            var _ = await context.AcquireTokenAsync(resourceUrl, clientCredential);
        }
    }
}
