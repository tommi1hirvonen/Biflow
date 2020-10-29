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

            // Create and start the pipeline execution.
            string runId;
            try
            {
                using HttpClient httpClient = new HttpClient();
                httpClient.BaseAddress = new Uri("https://management.azure.com");
                httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", AccessToken);
                var result = httpClient.PostAsync("/subscriptions/" + subscriptionId +
                    "/resourceGroups/" + resourceGroupName + "/providers/Microsoft.DataFactory/factories/" + dataFactoryName +
                    "/pipelines/" + PipelineName + "/createRun?api-version=2018-06-01", null).Result;
                if (result.IsSuccessStatusCode)
                {
                    string resultContent = result.Content.ReadAsStringAsync().Result;
                    var json = JsonSerializer.Deserialize<object>(resultContent) as JsonElement?;
                    runId = json.Value.GetProperty("runId").GetString();
                }
                else
                {
                    return new ExecutionResult.Failure("Error starting pipeline execution: " + result.ReasonPhrase);
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error starting pipeline {PipelineName} for Data Factory id {DataFactoryId}", PipelineName, DataFactoryId);
                throw ex;
            }

            // Loop and query the status until the pipeline has completed (end datetime is available).
            string runEnd = null;
            while (runEnd == null)
            {
                // During long running pipelines the access token may be expired. Check the token validity again.
                CheckAccessTokenValidity(tenantId, clientId, clientSecret);

                string status;
                string message;
                try
                {
                    using HttpClient httpClient = new HttpClient
                    {
                        BaseAddress = new Uri("https://management.azure.com")
                    };
                    httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", AccessToken);
                    var result = httpClient.GetAsync("/subscriptions/" + subscriptionId +
                        "/resourceGroups/" + resourceGroupName + "/providers/Microsoft.DataFactory/factories/" + dataFactoryName +
                        "/pipelineruns/" + runId + "?api-version=2018-06-01").Result;
                    if (result.IsSuccessStatusCode)
                    {
                        string resultContent = result.Content.ReadAsStringAsync().Result;
                        var json = JsonSerializer.Deserialize<object>(resultContent) as JsonElement?;
                        runEnd = json.Value.GetProperty("runEnd").GetString();
                        status = json.Value.GetProperty("status").GetString();
                        message = json.Value.GetProperty("message").GetString();
                    }
                    else
                    {
                        return new ExecutionResult.Failure("Error getting pipeline status: " + result.ReasonPhrase);
                    }
                }
                catch (Exception ex)
                {
                    Log.Error(ex, "Error getting pipeline {PipelineName} status for Data Factory id {DataFactoryId}", PipelineName, DataFactoryId);
                    return new ExecutionResult.Failure("Error getting pipeline status: " + ex.Message);
                }

                if (runEnd != null)
                {
                    if (status == "Succeeded")
                    {
                        return new ExecutionResult.Success();
                    }
                    else
                    {
                        return new ExecutionResult.Failure(message);
                    }
                }

                Thread.Sleep(5000);
            }

            return new ExecutionResult.Failure("Pipeline execution finished but no status information was fetched");
        }

        private void CheckAccessTokenValidity(string tenantId, string clientId, string clientSecret)
        {
            if (AccessTokenExpiresOn == null || DateTime.Now >= AccessTokenExpiresOn?.AddMinutes(-5)) // five minute safety margin
            {
                string expiresOnUnixTimestamp;
                try
                {
                    using HttpClient httpClient = new HttpClient
                    {
                        BaseAddress = new Uri("https://login.microsoftonline.com")
                    };
                    var content = new FormUrlEncodedContent(new[]
                    {
                        new KeyValuePair<string, string>("grant_type", "client_credentials"),
                        new KeyValuePair<string, string>("client_id", clientId),
                        new KeyValuePair<string, string>("client_secret", clientSecret),
                        new KeyValuePair<string, string>("resource", "https://management.azure.com/")
                    });

                    var result = httpClient.PostAsync("/" + tenantId + "/oauth2/token", content).Result;
                    string resultContent = result.Content.ReadAsStringAsync().Result;
                    var json = JsonSerializer.Deserialize<object>(resultContent) as JsonElement?;
                    AccessToken = json.Value.GetProperty("access_token").GetString();
                    expiresOnUnixTimestamp = json.Value.GetProperty("expires_on").GetString();
                }
                catch (Exception ex)
                {
                    Log.Error(ex, "Error getting Microsoft OAuth access token for Data Factory id {DataFactoryId}", DataFactoryId);
                    throw ex;
                }

                DateTime unixBase = new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc);
                AccessTokenExpiresOn = unixBase.AddSeconds(long.Parse(expiresOnUnixTimestamp)).ToLocalTime();

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
            }
        }

    }
}
