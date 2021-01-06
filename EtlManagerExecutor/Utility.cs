using System;
using System.Data;
using System.Data.SqlClient;
using System.Security.Cryptography;
using System.Text;

namespace EtlManagerExecutor
{

    public static class Utility
    {
        public static string GetEncryptionKey(string encryptionId, string connectionString)
        {
            using SqlConnection sqlConnection = new SqlConnection(connectionString);
            sqlConnection.Open();
            SqlCommand getKeyCmd = new SqlCommand("SELECT TOP 1 EncryptionKey, Entropy FROM etlmanager.EncryptionKey WHERE EncryptionId = @EncryptionId", sqlConnection);
            getKeyCmd.Parameters.AddWithValue("@EncryptionId", encryptionId);
            var reader = getKeyCmd.ExecuteReader();
            if (reader.Read())
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

        

        public static void OpenIfClosed(this SqlConnection sqlConnection)
        {
            if (sqlConnection.State != ConnectionState.Open && sqlConnection.State != ConnectionState.Connecting)
            {
                sqlConnection.Open();
            }
        }
    }
}
