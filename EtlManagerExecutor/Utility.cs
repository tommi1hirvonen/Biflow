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

        public static void OpenIfClosed(this SqlConnection sqlConnection)
        {
            if (sqlConnection.State != ConnectionState.Open && sqlConnection.State != ConnectionState.Connecting)
            {
                sqlConnection.Open();
            }
        }
    }
}
