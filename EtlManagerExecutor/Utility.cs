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
            var getKeyCmd = new SqlCommand("SELECT TOP 1 EncryptionKey, Entropy FROM etlmanager.EncryptionKey WHERE EncryptionId = @EncryptionId", sqlConnection);
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

        

        public static async Task OpenIfClosedAsync(this SqlConnection sqlConnection)
        {
            if (sqlConnection.State != ConnectionState.Open && sqlConnection.State != ConnectionState.Connecting)
            {
                await sqlConnection.OpenAsync();
            }
        }
    }
}
