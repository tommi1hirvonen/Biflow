using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using EtlManagerUi.Models;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;

namespace EtlManagerUi
{
    public static class Utility
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

        public static DateTime Trim(this DateTime date, long roundTicks)
        {
            return new DateTime(date.Ticks - date.Ticks % roundTicks, date.Kind);
        }

        public static string Left(this string value, int length)
        {
            if (value.Length > length)
            {
                return value.Substring(0, length);
            }

            return value;
        }

        public static DateTime ToDateTime(this long value)
        {
            return new DateTime(value);
        }

        public static bool ContainsIgnoreCase(this string source, string toCheck)
        {
            return source?.IndexOf(toCheck, StringComparison.OrdinalIgnoreCase) >= 0;
        }

        public async static Task<Guid> StartExecution(IConfiguration configuration, Job job, string username, List<string> stepIds = null)
        {
            using SqlConnection sqlConnection = new SqlConnection(configuration.GetConnectionString("EtlManagerContext"));
            SqlCommand sqlCommand;

            if (stepIds != null && stepIds.Count > 0)
            {
                sqlCommand = new SqlCommand(
                "EXEC [etlmanager].[ExecutionInitialize] @JobId = @JobId_, @Username = @Username_, @StepIds = @StepIds_"
                , sqlConnection);

                sqlCommand.Parameters.AddWithValue("@StepIds_", string.Join(',', stepIds));
            }
            else
            {
                sqlCommand = new SqlCommand(
                "EXEC [etlmanager].[ExecutionInitialize] @JobId = @JobId_, @Username = @Username_"
                , sqlConnection);
            }

            sqlCommand.Parameters.AddWithValue("@JobId_", job.JobId.ToString());
            sqlCommand.Parameters.AddWithValue("@Username_", username);

            await sqlConnection.OpenAsync();
            Guid executionId = (Guid)await sqlCommand.ExecuteScalarAsync();

            string executorPath = configuration.GetValue<string>("EtlManagerExecutorPath");

            ProcessStartInfo executionInfo = new ProcessStartInfo()
            {
                FileName = executorPath,
                ArgumentList = {
                    "execute",
                    "--id",
                    executionId.ToString()
                },
                // Set WorkingDirectory for the EtlManagerExecutor executable.
                // This way it reads the configuration file (appsettings.json) from the correct folder.
                WorkingDirectory = Path.GetDirectoryName(executorPath),
                UseShellExecute = true,
                CreateNoWindow = true,
                WindowStyle = ProcessWindowStyle.Hidden
            };

            Process executorProcess = new Process() { StartInfo = executionInfo };
            executorProcess.Start();

            return executionId;
        }

        public static bool StopJobExecution(IConfiguration configuration, Guid executionId, string username)
        {
            string executorPath = configuration.GetValue<string>("EtlManagerExecutorPath");

            ProcessStartInfo executionInfo = new ProcessStartInfo()
            {
                FileName = executorPath,
                ArgumentList = {
                    "cancel",
                    "--id",
                    executionId.ToString(),
                    "--username",
                    username
                },
                // Set WorkingDirectory for the EtlManagerExecutor executable.
                // This way it reads the configuration file (appsettings.json) from the correct folder.
                WorkingDirectory = Path.GetDirectoryName(executorPath),
                UseShellExecute = true,
                CreateNoWindow = true,
                WindowStyle = ProcessWindowStyle.Hidden
            };

            Process executorProcess = new Process() { StartInfo = executionInfo };
            executorProcess.Start();
            executorProcess.WaitForExit();
            int result = executorProcess.ExitCode;
            return result == 0;
        }

        public async static Task ToggleJobDependencyMode(IConfiguration configuration, Job job)
        {
            using SqlConnection sqlConnection = new SqlConnection(configuration.GetConnectionString("EtlManagerContext"));
            SqlCommand sqlCommand = new SqlCommand(
                @"UPDATE [etlmanager].[Job]
                SET [UseDependencyMode] = CASE [UseDependencyMode] WHEN 1 THEN 0 ELSE 1 END
                WHERE [JobId] = @JobId"
                , sqlConnection);
            sqlCommand.Parameters.AddWithValue("@JobId", job.JobId.ToString());
            await sqlConnection.OpenAsync();
            await sqlCommand.ExecuteNonQueryAsync();
        }

        public async static Task ToggleJobEnabled(IConfiguration configuration, Job job)
        {
            using SqlConnection sqlConnection = new SqlConnection(configuration.GetConnectionString("EtlManagerContext"));
            SqlCommand sqlCommand = new SqlCommand(
                @"UPDATE [etlmanager].[Job]
                SET [IsEnabled] = CASE [IsEnabled] WHEN 1 THEN 0 ELSE 1 END
                WHERE [JobId] = @JobId"
                , sqlConnection);
            sqlCommand.Parameters.AddWithValue("@JobId", job.JobId.ToString());
            await sqlConnection.OpenAsync();
            await sqlCommand.ExecuteNonQueryAsync();
        }

        public async static Task ToggleStepEnabled(IConfiguration configuration, Step step)
        {
            using SqlConnection sqlConnection = new SqlConnection(configuration.GetConnectionString("EtlManagerContext"));
            SqlCommand sqlCommand = new SqlCommand(
                @"UPDATE [etlmanager].[Step]
                SET [IsEnabled] = CASE [IsEnabled] WHEN 1 THEN 0 ELSE 1 END
                WHERE [StepId] = @StepId"
                , sqlConnection);
            sqlCommand.Parameters.AddWithValue("@StepId", step.StepId.ToString());
            await sqlConnection.OpenAsync();
            await sqlCommand.ExecuteNonQueryAsync();
        }

        public async static Task ToggleScheduleEnabled(IConfiguration configuration, Schedule schedule)
        {
            using SqlConnection sqlConnection = new SqlConnection(configuration.GetConnectionString("EtlManagerContext"));
            SqlCommand sqlCommand = new SqlCommand(
                @"UPDATE [etlmanager].[Schedule]
                SET [IsEnabled] = CASE [IsEnabled] WHEN 1 THEN 0 ELSE 1 END
                WHERE [ScheduleId] = @ScheduleId"
                , sqlConnection);
            sqlCommand.Parameters.AddWithValue("@ScheduleId", schedule.ScheduleId.ToString());
            await sqlConnection.OpenAsync();
            await sqlCommand.ExecuteNonQueryAsync();
        }

        public async static Task JobCopy(IConfiguration configuration, Guid jobId, string username)
        {
            using SqlConnection sqlConnection = new SqlConnection(configuration.GetConnectionString("EtlManagerContext"));
            SqlCommand sqlCommand = new SqlCommand(
                "EXEC [etlmanager].[JobCopy] @JobId = @JobId_, @Username = @Username_"
                , sqlConnection);
            sqlCommand.Parameters.AddWithValue("@JobId_", jobId.ToString());
            sqlCommand.Parameters.AddWithValue("@Username_", username);

            await sqlConnection.OpenAsync();
            await sqlCommand.ExecuteNonQueryAsync();
        }

        public async static Task StepCopy(IConfiguration configuration, Guid stepId, Guid targetJobId, string username)
        {
            using SqlConnection sqlConnection = new SqlConnection(configuration.GetConnectionString("EtlManagerContext"));
            SqlCommand sqlCommand = new SqlCommand(
                "EXEC [etlmanager].[StepCopy] @StepId = @StepId_, @TargetJobId = @TargetJobId_, @Username = @Username_"
                , sqlConnection);
            sqlCommand.Parameters.AddWithValue("@StepId_", stepId.ToString());
            sqlCommand.Parameters.AddWithValue("@TargetJobId_", targetJobId.ToString());
            sqlCommand.Parameters.AddWithValue("@Username_", username);

            await sqlConnection.OpenAsync();
            await sqlCommand.ExecuteNonQueryAsync();
        }

        public static bool IsEncryptionKeySet(IConfiguration configuration)
        {
            string connectionString = configuration.GetConnectionString("EtlManagerContext");
            string encryptionId = configuration.GetValue<string>("EncryptionId");
            using SqlConnection sqlConnection = new SqlConnection(connectionString);
            sqlConnection.Open();
            SqlCommand sqlCommand = new SqlCommand("SELECT * FROM etlmanager.EncryptionKey WHERE EncryptionId = @EncryptionId", sqlConnection);
            sqlCommand.Parameters.AddWithValue("@EncryptionId", encryptionId);
            var reader = sqlCommand.ExecuteReader();
            if (reader.HasRows)
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        public static string GetEncryptionKey(IConfiguration configuration)
        {
            string connectionString = configuration.GetConnectionString("EtlManagerContext");
            string encryptionId = configuration.GetValue<string>("EncryptionId");
            using SqlConnection sqlConnection = new SqlConnection(connectionString);
            sqlConnection.Open();
            SqlCommand getKeyCmd = new SqlCommand("SELECT TOP 1 EncryptionKey, Entropy FROM etlmanager.EncryptionKey WHERE EncryptionId = @EncryptionId", sqlConnection);
            getKeyCmd.Parameters.AddWithValue("@EncryptionId", encryptionId);
            var reader = getKeyCmd.ExecuteReader();
            if (reader.Read())
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

        public static void SetEncryptionKey(IConfiguration configuration, string oldEncryptionKey, string newEncryptionKey)
        {
            string connectionString = configuration.GetConnectionString("EtlManagerContext");
            string encryptionId = configuration.GetValue<string>("EncryptionId");
            using SqlConnection sqlConnection = new SqlConnection(connectionString);
            sqlConnection.Open();

            // Create random entropy
            byte[] entropy = new byte[16];
            new RNGCryptoServiceProvider().GetBytes(entropy);

            byte[] newEncryptionKeyBinary = Encoding.ASCII.GetBytes(newEncryptionKey);
#pragma warning disable CA1416 // Validate platform compatibility
            byte[] newEncryptionKeyEncrypted = ProtectedData.Protect(newEncryptionKeyBinary, entropy, DataProtectionScope.LocalMachine);
#pragma warning restore CA1416 // Validate platform compatibility
            SqlCommand updateKeyCmd = new SqlCommand(@"etlmanager.EncryptionKeySet
                    @EncryptionId = @EncryptionId_,
                    @OldEncryptionKey = @OldEncryptionKey_,
                    @NewEncryptionKey = @NewEncryptionKey_,
                    @NewEncryptionKeyEncrypted = @NewEncryptionKeyEncrypted_,
                    @Entropy = @Entropy_", sqlConnection);

            updateKeyCmd.Parameters.AddWithValue("@EncryptionId_", encryptionId);

            if (oldEncryptionKey != null) updateKeyCmd.Parameters.AddWithValue("@OldEncryptionKey_", oldEncryptionKey);
            else updateKeyCmd.Parameters.AddWithValue("@OldEncryptionKey_", DBNull.Value);

            updateKeyCmd.Parameters.AddWithValue("@NewEncryptionKey_", newEncryptionKey);
            updateKeyCmd.Parameters.AddWithValue("@NewEncryptionKeyEncrypted_", newEncryptionKeyEncrypted);
            updateKeyCmd.Parameters.AddWithValue("@Entropy_", entropy);

            updateKeyCmd.ExecuteNonQuery();
        }

        public static AuthenticationResult AuthenticateUser(IConfiguration configuration, string username, string password)
        {
            using SqlConnection sqlConnection = new SqlConnection(configuration.GetConnectionString("EtlManagerContext"));
            SqlCommand sqlCommand = new SqlCommand(
                "EXEC [etlmanager].[UserAuthenticate] @Username = @Username_, @Password = @Password_"
                , sqlConnection);
            sqlCommand.Parameters.AddWithValue("@Username_", username);
            sqlCommand.Parameters.AddWithValue("@Password_", password);

            sqlConnection.Open();
            object result = sqlCommand.ExecuteScalar();
            if (result is string role)
            {
                return new AuthenticationResult(role);
            }
            else
            {
                return new AuthenticationResult(null);
            }
        }

        public static async Task<bool> UpdatePasswordAsync(IConfiguration configuration, string username, string password)
        {
            using SqlConnection sqlConnection = new SqlConnection(configuration.GetConnectionString("EtlManagerContext"));
            SqlCommand sqlCommand = new SqlCommand(
                "EXEC [etlmanager].[UserUpdatePassword] @Username = @Username_, @Password = @Password_"
                , sqlConnection);
            sqlCommand.Parameters.AddWithValue("@Username_", username);
            sqlCommand.Parameters.AddWithValue("@Password_", password);

            sqlConnection.Open();
            int result = (int)(await sqlCommand.ExecuteScalarAsync());

            if (result > 0) return true;
            else return false;
        }

        public static async Task<bool> AddUserAsync(IConfiguration configuration, RoleUser user, string password)
        {
            using SqlConnection sqlConnection = new SqlConnection(configuration.GetConnectionString("EtlManagerContext"));
            SqlCommand sqlCommand = new SqlCommand(
                "EXEC [etlmanager].[UserAdd] @Username = @Username_, @Password = @Password_, @Role = @Role_, @Email = @Email_"
                , sqlConnection);
            sqlCommand.Parameters.AddWithValue("@Username_", user.Username);
            sqlCommand.Parameters.AddWithValue("@Password_", password);
            sqlCommand.Parameters.AddWithValue("@Role_", user.Role);
            if (user.Email != null)
            {
                sqlCommand.Parameters.AddWithValue("@Email_", user.Email);
            }
            else
            {
                sqlCommand.Parameters.AddWithValue("@Email_", DBNull.Value);
            }


            await sqlConnection.OpenAsync();
            int result = (int)(await sqlCommand.ExecuteScalarAsync());

            if (result > 0) return true;
            else return false;
        }

        public static DateTime StartOfWeek(this DateTime dt, DayOfWeek startOfWeek)
        {
            int diff = (7 + (dt.DayOfWeek - startOfWeek)) % 7;
            return dt.AddDays(-1 * diff).Date;
        }

    }

    public class AuthenticationResult
    {
        public bool AuthenticationSuccessful { get; }
        public string Role { get; }
        public AuthenticationResult(string role)
        {
            Role = role;
            switch (role)
            {
                case "Admin":
                case "Editor":
                case "Operator":
                case "Viewer":
                    AuthenticationSuccessful = true;
                    return;
                default:
                    AuthenticationSuccessful = false;
                    return;
            }
        }
    }
}
