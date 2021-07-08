using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.IO;
using System.IO.Pipes;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace EtlManagerUtils
{
    public static class CommonUtility
    {
        public static string SecondsToReadableFormat(this int value)
        {
            var duration = TimeSpan.FromSeconds(value);
            var result = "";
            var days = duration.Days;
            var hours = duration.Hours;
            var minutes = duration.Minutes;
            var seconds = duration.Seconds;
            if (days > 0) result += days + " d ";
            if (hours > 0 || days > 0) result += hours + " h ";
            if (minutes > 0 || hours > 0 || days > 0) result += minutes + " min ";
            result += seconds + " s";
            return result;
        }

        public static byte[] ReadMessage(PipeStream pipe)
        {
            byte[] buffer = new byte[1024];
            using var ms = new MemoryStream();
            do
            {
                var readBytes = pipe.Read(buffer, 0, buffer.Length);
                ms.Write(buffer, 0, readBytes);
            }
            while (!pipe.IsMessageComplete);

            return ms.ToArray();
        }

        public static async Task<string?> GetEncryptionKeyAsync(IConfiguration configuration)
        {
            string connectionString = configuration.GetConnectionString("EtlManagerContext");
            string encryptionId = configuration.GetValue<string>("EncryptionId");
            return await GetEncryptionKeyAsync(encryptionId, connectionString);
        }

        public static async Task<string?> GetEncryptionKeyAsync(string encryptionId, string connectionString)
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

                #pragma warning disable CA1416 // Validate platform compatibility
                byte[] output = ProtectedData.Unprotect(encryptionKeyBinary, entropy, DataProtectionScope.LocalMachine);
                #pragma warning restore CA1416 // Validate platform compatibility
                return Encoding.ASCII.GetString(output);
            }
            else
            {
                return null;
            }
        }

        public static async Task SetEncryptionKeyAsync(IConfiguration configuration, string? oldEncryptionKey, string newEncryptionKey)
        {
            string connectionString = configuration.GetConnectionString("EtlManagerContext");
            string encryptionId = configuration.GetValue<string>("EncryptionId");
            using var sqlConnection = new SqlConnection(connectionString);
            await sqlConnection.OpenAsync();

            // Create random entropy
            byte[] entropy = new byte[16];
            new RNGCryptoServiceProvider().GetBytes(entropy);

            byte[] newEncryptionKeyBinary = Encoding.ASCII.GetBytes(newEncryptionKey);

            #pragma warning disable CA1416 // Validate platform compatibility
            byte[] newEncryptionKeyEncrypted = ProtectedData.Protect(newEncryptionKeyBinary, entropy, DataProtectionScope.LocalMachine);
            #pragma warning restore CA1416 // Validate platform compatibility

            using var updateKeyCmd = new SqlCommand(@"etlmanager.EncryptionKeySet
                    @EncryptionId = @EncryptionId_,
                    @OldEncryptionKey = @OldEncryptionKey_,
                    @NewEncryptionKey = @NewEncryptionKey_,
                    @NewEncryptionKeyEncrypted = @NewEncryptionKeyEncrypted_,
                    @Entropy = @Entropy_", sqlConnection);

            updateKeyCmd.Parameters.AddWithValue("@EncryptionId_", encryptionId);

            if (oldEncryptionKey is not null) updateKeyCmd.Parameters.AddWithValue("@OldEncryptionKey_", oldEncryptionKey);
            else updateKeyCmd.Parameters.AddWithValue("@OldEncryptionKey_", DBNull.Value);

            updateKeyCmd.Parameters.AddWithValue("@NewEncryptionKey_", newEncryptionKey);
            updateKeyCmd.Parameters.AddWithValue("@NewEncryptionKeyEncrypted_", newEncryptionKeyEncrypted);
            updateKeyCmd.Parameters.AddWithValue("@Entropy_", entropy);

            await updateKeyCmd.ExecuteNonQueryAsync();
        }
    }
}
