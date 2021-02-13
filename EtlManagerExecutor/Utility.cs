using Serilog;
using System;
using System.Data;
using System.Data.SqlClient;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace EtlManagerExecutor
{

    public static class Utility
    {
        public static async Task<string> GetEncryptionKeyAsync(string encryptionId, string connectionString)
        {
            using var sqlConnection = new SqlConnection(connectionString);
            await sqlConnection.OpenAsync();
            using var getKeyCmd = new SqlCommand("SELECT TOP 1 EncryptionKey, Entropy FROM etlmanager.EncryptionKey WHERE EncryptionId = @EncryptionId", sqlConnection);
            getKeyCmd.Parameters.AddWithValue("@EncryptionId", encryptionId);
            using var reader = await getKeyCmd.ExecuteReaderAsync();
            if (await reader.ReadAsync())
            {
                byte[] encryptionKeyBinary = (byte[])reader["EncryptionKey"];
                byte[] entropy = (byte[])reader["Entropy"];

                byte[] output = ProtectedData.Unprotect(encryptionKeyBinary, entropy, DataProtectionScope.LocalMachine);
                return Encoding.ASCII.GetString(output);
            }
            else
            {
                return null;
            }
        }

        public static async Task UpdateErrorMessageAsync(ExecutionConfiguration executionConfig, string errorMessage)
        {
            using var sqlConnection = new SqlConnection(executionConfig.ConnectionString);
            using var sqlCommand = new SqlCommand(
                    @"UPDATE etlmanager.Execution
                    SET ExecutionStatus = 'FAILED', ErrorMessage = @ErrorMessage, StartDateTime = GETDATE(), EndDateTime = GETDATE()
                    WHERE ExecutionId = @ExecutionId"
                    , sqlConnection)
            {
                CommandTimeout = 120 // two minutes
            };
            sqlCommand.Parameters.AddWithValue("@ErrorMessage", errorMessage);
            sqlCommand.Parameters.AddWithValue("@ExecutionId", executionConfig.ExecutionId);
            try
            {
                await sqlConnection.OpenAsync();
                await sqlCommand.ExecuteNonQueryAsync();
            }
            catch (Exception ex)
            {
                Log.Error(ex, "{ExecutionId} Error updating error message", executionConfig.ExecutionId);
            }
        }

    }
}
