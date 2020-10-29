using Microsoft.Azure.Management.DataFactory;
using Microsoft.Azure.Management.DataFactory.Models;
using Microsoft.IdentityModel.Clients.ActiveDirectory;
using Microsoft.Rest;
using Serilog;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;

namespace EtlManagerExecutor
{
    class PipelineExecution
    {
        private string EtlManagerConnectionString { get; set; }
        private string DataFactoryId { get; set; }

        private string AccessToken { get; set; }
        private DateTime? AccessTokenExpiresOn { get; set; }

        private string PipelineName { get; set; }

        public PipelineExecution(string etlManagerConnectionString, string dataFactoryId, string pipelineName)
        {
            EtlManagerConnectionString = etlManagerConnectionString;
            DataFactoryId = dataFactoryId;
            PipelineName = pipelineName;
        }

        public ExecutionResult Run()
        {
            string tenantId;
            string subscriptionId;
            string resourceGroupName;
            string dataFactoryName;
            string clientId;
            string clientSecret;

            // Get the target Data Factory information from the database.
            try
            {
                using SqlConnection sqlConnection = new SqlConnection(EtlManagerConnectionString);
                sqlConnection.Open();
                SqlCommand sqlCommand = new SqlCommand(
                    @"SELECT [TenantId], [SubscriptionId], [ClientId], [ClientSecret],
                        [ResourceGroupName], [ResourceName], [AccessToken], [AccessTokenExpiresOn]
                FROM etlmanager.DataFactory
                WHERE DataFactoryId = @DataFactoryId", sqlConnection);
                sqlCommand.Parameters.AddWithValue("@DataFactoryId", DataFactoryId);
                using var reader = sqlCommand.ExecuteReader();
                reader.Read();
                tenantId = reader["TenantId"].ToString();
                subscriptionId = reader["SubscriptionId"].ToString();
                resourceGroupName = reader["ResourceGroupName"].ToString();
                dataFactoryName = reader["ResourceName"].ToString();
                clientId = reader["ClientId"].ToString();
                clientSecret = reader["ClientSecret"].ToString();
                if (reader["AccessToken"] != DBNull.Value) AccessToken = reader["AccessToken"].ToString();
                if (reader["AccessTokenExpiresOn"] != DBNull.Value) AccessTokenExpiresOn = (DateTime)reader["AccessTokenExpiresOn"];
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error getting Data Factory information for id {DataFactoryId}", DataFactoryId);
                throw ex;
            }

            // Check if the current access token is valid and get a new one if not.
            CheckAccessTokenValidity(tenantId, clientId, clientSecret);

            var credentials = new TokenCredentials(AccessToken);
            var client = new DataFactoryManagementClient(credentials) { SubscriptionId = subscriptionId };

            CreateRunResponse createRunResponse;
            try
            {
                createRunResponse = client.Pipelines.CreateRun(resourceGroupName, dataFactoryName, PipelineName);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error creating pipeline for Data Factory id {DataFactoryId} and pipeline {PipelineName}", DataFactoryId, PipelineName);
                throw ex;
            }

            string runId = createRunResponse.RunId;

            PipelineRun pipelineRun;
            while (true)
            {
                if (!CheckAccessTokenValidity(tenantId, clientId, clientSecret))
                {
                    credentials = new TokenCredentials(AccessToken);
                    client = new DataFactoryManagementClient(credentials) { SubscriptionId = subscriptionId };
                }

                try
                {
                    pipelineRun = client.PipelineRuns.Get(resourceGroupName, dataFactoryName, runId);
                }
                catch (Exception ex)
                {
                    Log.Error(ex, "Error getting pipeline run status for Data Factory id {DataFactoryId}, pipeline {PipelineName}, run id {runId}", DataFactoryId, PipelineName, runId);
                    throw ex;
                }

                if (pipelineRun.Status == "InProgress" || pipelineRun.Status == "Queued")
                {
                    Thread.Sleep(5000);
                }
                else
                {
                    break;
                }
            }

            if (pipelineRun.Status == "Succeeded")
            {
                return new ExecutionResult.Success();
            }
            else
            {
                return new ExecutionResult.Failure(pipelineRun.Message);
            }
        }

        private bool CheckAccessTokenValidity(string tenantId, string clientId, string clientSecret)
        {
            if (AccessTokenExpiresOn == null || DateTime.Now >= AccessTokenExpiresOn?.AddMinutes(-5)) // five minute safety margin
            {
                try
                {
                    var context = new AuthenticationContext("https://login.microsoftonline.com/" + tenantId);
                    var clientCredential = new ClientCredential(clientId, clientSecret);
                    var result = context.AcquireTokenAsync("https://management.azure.com/", clientCredential).Result;
                    AccessToken = result.AccessToken;
                    AccessTokenExpiresOn = result.ExpiresOn.ToLocalTime().DateTime;
                }
                catch (Exception ex)
                {
                    Log.Error(ex, "Error getting Microsoft OAuth access token for Data Factory id {DataFactoryId}", DataFactoryId);
                    throw ex;
                }

                // Update the token and its expiration time to the database for later use.
                try
                {
                    using SqlConnection sqlConnection = new SqlConnection(EtlManagerConnectionString);
                    sqlConnection.Open();
                    SqlCommand updateTokenCmd = new SqlCommand(
                        @"UPDATE etlmanager.DataFactory
                    SET AccessToken = @AccessToken, AccessTokenExpiresOn = @AccessTokenExpiresOn
                    WHERE ClientId = @ClientId AND ClientSecret = @ClientSecret", sqlConnection);
                    updateTokenCmd.Parameters.AddWithValue("@AccessToken", AccessToken);
                    updateTokenCmd.Parameters.AddWithValue("@AccessTokenExpiresOn", AccessTokenExpiresOn);
                    updateTokenCmd.Parameters.AddWithValue("@ClientId", clientId);
                    updateTokenCmd.Parameters.AddWithValue("@ClientSecret", clientSecret);
                    updateTokenCmd.ExecuteNonQuery();
                }
                catch (Exception ex)
                {
                    Log.Error(ex, "Error updating the OAuth access token for Data Factory id {DataFactoryId}", DataFactoryId);
                    throw ex;
                }
                return false;
            }
            else
            {
                return true;
            }
        }

    }
}
