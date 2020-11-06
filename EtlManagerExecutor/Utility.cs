using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Clients.ActiveDirectory;
using Serilog;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Security.Cryptography;
using System.Text;

namespace EtlManagerExecutor
{
    public abstract class ExecutionResult
    {
        public class Success : ExecutionResult { }
        public class Failure : ExecutionResult
        {
            public string ErrorMessage { get; } = string.Empty;
            public Failure(string errorMessage)
            {
                ErrorMessage = errorMessage;
            }
        }
    }

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

        public bool CheckAccessTokenValidity(string connectionString)
        {
            if (AccessTokenExpiresOn == null || DateTime.Now >= AccessTokenExpiresOn?.AddMinutes(-5)) // five minute safety margin
            {
                try
                {
                    var context = new AuthenticationContext("https://login.microsoftonline.com/" + TenantId);
                    var clientCredential = new ClientCredential(ClientId, ClientSecret);
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
                    using SqlConnection sqlConnection = new SqlConnection(connectionString);
                    sqlConnection.Open();
                    SqlCommand updateTokenCmd = new SqlCommand(
                        @"UPDATE etlmanager.DataFactory
                    SET AccessToken = @AccessToken, AccessTokenExpiresOn = @AccessTokenExpiresOn
                    WHERE ClientId = @ClientId AND ClientSecret = @ClientSecret", sqlConnection);
                    updateTokenCmd.Parameters.AddWithValue("@AccessToken", AccessToken);
                    updateTokenCmd.Parameters.AddWithValue("@AccessTokenExpiresOn", AccessTokenExpiresOn);
                    updateTokenCmd.Parameters.AddWithValue("@ClientId", ClientId);
                    updateTokenCmd.Parameters.AddWithValue("@ClientSecret", ClientSecret);
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

    public static class Utility
    {
        public static string GetEncryptionKey(string connectionString)
        {
            using SqlConnection sqlConnection = new SqlConnection(connectionString);
            sqlConnection.Open();
            SqlCommand getKeyCmd = new SqlCommand("SELECT TOP 1 EncryptionKey, Entropy FROM etlmanager.EncryptionKey", sqlConnection);
            var reader = getKeyCmd.ExecuteReader();
            if (reader.Read())
            {
                byte[] encryptionKeyBinary = (byte[])reader["EncryptionKey"];
                byte[] entropy = (byte[])reader["Entropy"];

                byte[] output = ProtectedData.Unprotect(encryptionKeyBinary, entropy, DataProtectionScope.CurrentUser);
                return Encoding.ASCII.GetString(output);
            }
            else
            {
                return null;
            }
        }

        public static DataFactory GetDataFactory(string connectionString, string dataFactoryId, string encryptionKey)
        {
            using SqlConnection sqlConnection = new SqlConnection(connectionString);
            sqlConnection.Open();
            SqlCommand sqlCommand = new SqlCommand(
                @"SELECT [TenantId], [SubscriptionId], [ClientId], CONVERT(NVARCHAR(MAX), DECRYPTBYPASSPHRASE(@EncryptionKey, ClientSecret)) AS ClientSecret,
                        [ResourceGroupName], [ResourceName], [AccessToken], [AccessTokenExpiresOn]
                FROM etlmanager.DataFactory
                WHERE DataFactoryId = @DataFactoryId", sqlConnection);
            sqlCommand.Parameters.AddWithValue("@DataFactoryId", dataFactoryId);
            sqlCommand.Parameters.AddWithValue("@EncryptionKey", encryptionKey);

            using var reader = sqlCommand.ExecuteReader();
            reader.Read();
            
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

        public static void OpenIfClosed(this SqlConnection sqlConnection)
        {
            if (sqlConnection.State != ConnectionState.Open && sqlConnection.State != ConnectionState.Connecting)
            {
                sqlConnection.Open();
            }
        }
    }
}
