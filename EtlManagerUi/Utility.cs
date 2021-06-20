using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Pipes;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using EtlManagerUi.Models;
using EtlManagerUtils;
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

        public static string FormatPercentage(this decimal value, int decimalPlaces)
        {
            return decimal.Round(value, decimalPlaces).ToString() + "%";
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

        public async static Task<Guid> StartExecutionAsync(IConfiguration configuration, Job job, string username, List<string> stepIds = null, bool notify = false)
        {
            Guid executionId;
            using (var sqlConnection = new SqlConnection(configuration.GetConnectionString("EtlManagerContext")))
            {
                using var sqlCommand = new SqlCommand
                {
                    Connection = sqlConnection
                };

                if (stepIds is not null && stepIds.Count > 0)
                {
                    sqlCommand.CommandText = "EXEC [etlmanager].[ExecutionInitialize] @JobId = @JobId_, @Username = @Username_, @StepIds = @StepIds_";
                    sqlCommand.Parameters.AddWithValue("@StepIds_", string.Join(',', stepIds));
                }
                else
                {
                    sqlCommand.CommandText = "EXEC [etlmanager].[ExecutionInitialize] @JobId = @JobId_, @Username = @Username_";
                }

                sqlCommand.Parameters.AddWithValue("@JobId_", job.JobId.ToString());
                sqlCommand.Parameters.AddWithValue("@Username_", username);

                await sqlConnection.OpenAsync();
                executionId = (Guid)await sqlCommand.ExecuteScalarAsync();
            }

            string executorPath = configuration.GetValue<string>("EtlManagerExecutorPath");

            var executionInfo = new ProcessStartInfo()
            {
                // The installation folder should be included in the Path variable, so no path required here.
                FileName = executorPath,
                ArgumentList = {
                    "execute",
                    "--id",
                    executionId.ToString(),
                    notify ? "--notify" : ""
                },
                UseShellExecute = false,
                CreateNoWindow = true,
                WindowStyle = ProcessWindowStyle.Hidden
            };

            var executorProcess = new Process() { StartInfo = executionInfo };
            executorProcess.Start();

            return executionId;
        }

        public static async Task StopExecutionAsync(Guid executionId, string username, Guid? stepId = null)
        {
            // Connect to the pipe server set up by the executor process.
            using var pipeClient = new NamedPipeClientStream(".", executionId.ToString().ToLower(), PipeDirection.Out); // "." => the pipe server is on the same computer
            await pipeClient.ConnectAsync(10000); // wait for 10 seconds
            using var streamWriter = new StreamWriter(pipeClient);
            // Send cancel command.
            var username_ = string.IsNullOrWhiteSpace(username) ? "unknown" : username;
            var stepId_ = stepId?.ToString().ToLower();
            var cancelCommand = new { StepId = stepId_, Username = username_ };
            var json = JsonSerializer.Serialize(cancelCommand);
            streamWriter.WriteLine(json);
        }

        public async static Task ToggleJobDependencyModeAsync(IConfiguration configuration, Job job, bool enabled)
        {
            int value = enabled ? 1 : 0;
            using var sqlConnection = new SqlConnection(configuration.GetConnectionString("EtlManagerContext"));
            using var sqlCommand = new SqlCommand(
                @"UPDATE [etlmanager].[Job]
                SET [UseDependencyMode] = @Value
                WHERE [JobId] = @JobId"
                , sqlConnection);
            sqlCommand.Parameters.AddWithValue("@JobId", job.JobId.ToString());
            sqlCommand.Parameters.AddWithValue("@Value", value);
            await sqlConnection.OpenAsync();
            await sqlCommand.ExecuteNonQueryAsync();
        }

        public async static Task ToggleJobEnabledAsync(IConfiguration configuration, Job job, bool enabled)
        {
            int value = enabled ? 1 : 0;
            using var sqlConnection = new SqlConnection(configuration.GetConnectionString("EtlManagerContext"));
            using var sqlCommand = new SqlCommand(
                @"UPDATE [etlmanager].[Job]
                SET [IsEnabled] = @Value
                WHERE [JobId] = @JobId"
                , sqlConnection);
            sqlCommand.Parameters.AddWithValue("@JobId", job.JobId.ToString());
            sqlCommand.Parameters.AddWithValue("@Value", value);
            await sqlConnection.OpenAsync();
            await sqlCommand.ExecuteNonQueryAsync();
        }

        public async static Task ToggleStepEnabledAsync(IConfiguration configuration, Step step, bool enabled)
        {
            int value = enabled ? 1 : 0;
            using var sqlConnection = new SqlConnection(configuration.GetConnectionString("EtlManagerContext"));
            using var sqlCommand = new SqlCommand(
                @"UPDATE [etlmanager].[Step]
                SET [IsEnabled] = @Value
                WHERE [StepId] = @StepId"
                , sqlConnection);
            sqlCommand.Parameters.AddWithValue("@StepId", step.StepId.ToString());
            sqlCommand.Parameters.AddWithValue("@Value", value);
            await sqlConnection.OpenAsync();
            await sqlCommand.ExecuteNonQueryAsync();
        }

        public async static Task RemoveTagAsync(IConfiguration configuration, Step step, Tag tag)
        {
            using var sqlConnection = new SqlConnection(configuration.GetConnectionString("EtlManagerContext"));
            using var sqlCommand = new SqlCommand(
                @"DELETE FROM [etlmanager].[StepTag]
                WHERE [StepsStepId] = @StepId AND [TagsTagId] = @TagId"
                , sqlConnection);
            sqlCommand.Parameters.AddWithValue("@StepId", step.StepId.ToString());
            sqlCommand.Parameters.AddWithValue("@TagId", tag.TagId);
            await sqlConnection.OpenAsync();
            await sqlCommand.ExecuteNonQueryAsync();
        }

        public async static Task AddTagAsync(IConfiguration configuration, Step step, Tag tag)
        {
            using var sqlConnection = new SqlConnection(configuration.GetConnectionString("EtlManagerContext"));
            await sqlConnection.OpenAsync();

            // Get id for existing tag.
            using var cmdGetGuid = new SqlCommand("SELECT TagId FROM etlmanager.Tag WHERE TagName = @TagName_", sqlConnection);
            cmdGetGuid.Parameters.AddWithValue("@TagName_", tag.TagName);
            var guid = (Guid?)await cmdGetGuid.ExecuteScalarAsync();
            
            // If id is null, then no tag with matching name was found. Insert a new tag.
            if (guid is null)
            {
                guid = Guid.NewGuid();
                using var cmdInsertTag = new SqlCommand("INSERT INTO etlmanager.Tag (TagId, TagName) SELECT @TagId, @TagName", sqlConnection);
                cmdInsertTag.Parameters.AddWithValue("@TagId", guid);
                cmdInsertTag.Parameters.AddWithValue("@TagName", tag.TagName);
                await cmdInsertTag.ExecuteNonQueryAsync();
            }

            tag.TagId = (Guid)guid;

            // Insert a link between the given step and tag.
            using var sqlCommand = new SqlCommand(
                @"INSERT INTO etlmanager.StepTag (StepsStepId, TagsTagId)
                SELECT @StepId, @TagId
                WHERE NOT EXISTS (SELECT * FROM etlmanager.StepTag WHERE StepsStepId = @StepId AND TagsTagId = @TagId)"
                , sqlConnection);
            sqlCommand.Parameters.AddWithValue("@StepId", step.StepId);
            sqlCommand.Parameters.AddWithValue("@TagId", guid);
            await sqlCommand.ExecuteNonQueryAsync();
        }

        public async static Task<bool> SchedulerServiceDeleteJobAsync(Job job)
        {
            // Connect to the pipe server set up by the scheduler service.
            using var pipeClient = new NamedPipeClientStream(".", "ETL Manager Scheduler", PipeDirection.InOut); // "." => the pipe server is on the same computer
            await pipeClient.ConnectAsync(10000); // wait for 10 seconds
#pragma warning disable CA1416 // Validate platform compatibility
            pipeClient.ReadMode = PipeTransmissionMode.Message; // Each byte array is transferred as a single message
#pragma warning restore CA1416 // Validate platform compatibility

            // Send add command.
            var addCommand = new SchedulerCommand(SchedulerCommand.CommandType.Delete, job.JobId.ToString(), null, null);
            var json = JsonSerializer.Serialize(addCommand);
            var bytes = Encoding.UTF8.GetBytes(json);
            pipeClient.Write(bytes, 0, bytes.Length);


            // Get response from scheduler service
            var responseBytes = CommonUtility.ReadMessage(pipeClient);
            var response = Encoding.UTF8.GetString(responseBytes);
            return response == "SUCCESS";
        }

        public async static Task<bool> SchedulerServiceSendCommandAsync(SchedulerCommand.CommandType commandType, Schedule schedule)
        {
            // Connect to the pipe server set up by the scheduler service.
            using var pipeClient = new NamedPipeClientStream(".", "ETL Manager Scheduler", PipeDirection.InOut); // "." => the pipe server is on the same computer
            await pipeClient.ConnectAsync(10000); // wait for 10 seconds
#pragma warning disable CA1416 // Validate platform compatibility
            pipeClient.ReadMode = PipeTransmissionMode.Message; // Each byte array is transferred as a single message
#pragma warning restore CA1416 // Validate platform compatibility

            // Send add command.
            var addCommand = new SchedulerCommand(commandType, schedule.JobId.ToString(), schedule.ScheduleId.ToString(), schedule.CronExpression);
            var json = JsonSerializer.Serialize(addCommand);
            var bytes = Encoding.UTF8.GetBytes(json);
            pipeClient.Write(bytes, 0, bytes.Length);


            // Get response from scheduler service
            var responseBytes = CommonUtility.ReadMessage(pipeClient);
            var response = Encoding.UTF8.GetString(responseBytes);
            return response == "SUCCESS";
        }

        public async static Task<bool> ToggleScheduleEnabledAsync(IConfiguration configuration, Schedule schedule, bool enabled)
        {
            int value = enabled ? 1 : 0;
            using var sqlConnection = new SqlConnection(configuration.GetConnectionString("EtlManagerContext"));
            
            await sqlConnection.OpenAsync();
            using var transaction = sqlConnection.BeginTransaction();
            using var sqlCommand = new SqlCommand(
                @"UPDATE [etlmanager].[Schedule]
                SET [IsEnabled] = @Value
                WHERE [ScheduleId] = @ScheduleId"
                , sqlConnection, transaction);
            sqlCommand.Parameters.AddWithValue("@ScheduleId", schedule.ScheduleId.ToString());
            sqlCommand.Parameters.AddWithValue("@Value", value);
            await sqlCommand.ExecuteNonQueryAsync();
            var commandType = enabled ? SchedulerCommand.CommandType.Resume : SchedulerCommand.CommandType.Pause;
            bool success = await SchedulerServiceSendCommandAsync(commandType, schedule);
            if (success)
            {
                transaction.Commit();
                return true;
            }
            else
            {
                transaction.Rollback();
                return false;
            }
        }

        public async static Task<Guid> JobCopyAsync(IConfiguration configuration, Guid jobId, string username)
        {
            using var sqlConnection = new SqlConnection(configuration.GetConnectionString("EtlManagerContext"));
            using var sqlCommand = new SqlCommand(
                "EXEC [etlmanager].[JobCopy] @JobId = @JobId_, @Username = @Username_"
                , sqlConnection);
            sqlCommand.Parameters.AddWithValue("@JobId_", jobId.ToString());
            sqlCommand.Parameters.AddWithValue("@Username_", username);

            await sqlConnection.OpenAsync();
            var createdJobId = (Guid)await sqlCommand.ExecuteScalarAsync();
            return createdJobId;
        }

        public async static Task<Guid> StepCopyAsync(IConfiguration configuration, Guid stepId, Guid targetJobId, string username)
        {
            using var sqlConnection = new SqlConnection(configuration.GetConnectionString("EtlManagerContext"));
            using var sqlCommand = new SqlCommand(
                "EXEC [etlmanager].[StepCopy] @StepId = @StepId_, @TargetJobId = @TargetJobId_, @Username = @Username_"
                , sqlConnection);
            sqlCommand.Parameters.AddWithValue("@StepId_", stepId.ToString());
            sqlCommand.Parameters.AddWithValue("@TargetJobId_", targetJobId.ToString());
            sqlCommand.Parameters.AddWithValue("@Username_", username);

            await sqlConnection.OpenAsync();
            var createdStepId = (Guid)await sqlCommand.ExecuteScalarAsync();
            return createdStepId;
        }

        public static async Task<bool> IsEncryptionKeySetAsync(IConfiguration configuration)
        {
            string connectionString = configuration.GetConnectionString("EtlManagerContext");
            string encryptionId = configuration.GetValue<string>("EncryptionId");
            using var sqlConnection = new SqlConnection(connectionString);
            using var sqlCommand = new SqlCommand("SELECT * FROM etlmanager.EncryptionKey WHERE EncryptionId = @EncryptionId", sqlConnection);
            sqlCommand.Parameters.AddWithValue("@EncryptionId", encryptionId);
            await sqlConnection.OpenAsync();
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


        public static AuthenticationResult AuthenticateUser(IConfiguration configuration, string username, string password)
        {
            using var sqlConnection = new SqlConnection(configuration.GetConnectionString("EtlManagerContext"));
            using var sqlCommand = new SqlCommand(
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

        public static async Task<Dictionary<string, Dictionary<string, List<string>>>> GetSSISCatalogPackages(IConfiguration configuration, string connectionId)
        {
            Dictionary<string, Dictionary<string, List<string>>> catalog = new();
            var encryptionKey = await CommonUtility.GetEncryptionKeyAsync(configuration);
            string catalogConnectionString;
            using (var sqlConnection = new SqlConnection(configuration.GetConnectionString("EtlManagerContext")))
            {
                await sqlConnection.OpenAsync();
                using var sqlCommand = new SqlCommand("SELECT etlmanager.GetConnectionStringDecrypted(@ConnectionId, @EncryptionKey) AS ConnectionString", sqlConnection);
                sqlCommand.Parameters.AddWithValue("@EncryptionKey", encryptionKey);
                sqlCommand.Parameters.AddWithValue("@ConnectionId", connectionId);
                catalogConnectionString = (await sqlCommand.ExecuteScalarAsync()).ToString();
            }
            using (var sqlConnection = new SqlConnection(catalogConnectionString))
            {
                await sqlConnection.OpenAsync();
                using var sqlCommand = new SqlCommand(
                    @"SELECT
	                    [folders].[name] AS FolderName,
	                    [projects].[name] AS ProjectName,
	                    [packages].[name] AS PackageName
                    FROM [SSISDB].[catalog].[folders]
	                    LEFT JOIN [SSISDB].[catalog].[projects] ON [folders].[folder_id] = [projects].[folder_id]
	                    LEFT JOIN [SSISDB].[catalog].[packages] ON [projects].[project_id] = [packages].[project_id]
                    ORDER BY FolderName, ProjectName, PackageName"
                    , sqlConnection);
                using var reader = await sqlCommand.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    var folder = reader["FolderName"].ToString();
                    var project = reader["ProjectName"].ToString();
                    var package = reader["PackageName"].ToString();
                    if (!catalog.ContainsKey(folder))
                    {
                        catalog[folder] = new();
                    }
                    if (!catalog[folder].ContainsKey(project))
                    {
                        catalog[folder][project] = new();
                    }
                    if (package is not null)
                    {
                        catalog[folder][project].Add(package);
                    }
                }
            }
            return catalog;
        }

        public static async Task<Dictionary<string, List<string>>> GetDatabaseStoredProcedures(IConfiguration configuration, string connectionId)
        {
            var procedures = new Dictionary<string, List<string>>();
            var encryptionKey = await CommonUtility.GetEncryptionKeyAsync(configuration);
            string connectionString;
            using (var sqlConnection = new SqlConnection(configuration.GetConnectionString("EtlManagerContext")))
            {
                await sqlConnection.OpenAsync();
                using var sqlCommand = new SqlCommand("SELECT etlmanager.GetConnectionStringDecrypted(@ConnectionId, @EncryptionKey) AS ConnectionString", sqlConnection);
                sqlCommand.Parameters.AddWithValue("@EncryptionKey", encryptionKey);
                sqlCommand.Parameters.AddWithValue("@ConnectionId", connectionId);
                connectionString = (await sqlCommand.ExecuteScalarAsync()).ToString();
            }
            using (var sqlConnection = new SqlConnection(connectionString))
            {
                await sqlConnection.OpenAsync();
                using var sqlCommand = new SqlCommand(
                    @"SELECT OBJECT_SCHEMA_NAME([object_id]) AS [schema], [name]
                    FROM [sys].[procedures]
                    ORDER BY [schema], [name]"
                    , sqlConnection);
                using var reader = await sqlCommand.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    var schema = reader["schema"].ToString();
                    var procedure = reader["name"].ToString();
                    if (!procedures.ContainsKey(schema))
                    {
                        procedures[schema] = new();
                    }
                    procedures[schema].Add(procedure);
                }
            }
            return procedures;
        }

        public static async Task<bool> UpdatePasswordAsync(IConfiguration configuration, string username, string password)
        {
            using var sqlConnection = new SqlConnection(configuration.GetConnectionString("EtlManagerContext"));
            using var sqlCommand = new SqlCommand(
                "EXEC [etlmanager].[UserUpdatePassword] @Username = @Username_, @Password = @Password_"
                , sqlConnection);
            sqlCommand.Parameters.AddWithValue("@Username_", username);
            sqlCommand.Parameters.AddWithValue("@Password_", password);

            await sqlConnection.OpenAsync();
            int result = (int)(await sqlCommand.ExecuteScalarAsync());

            if (result > 0) return true;
            else return false;
        }

        public static async Task<bool> AddUserAsync(IConfiguration configuration, RoleUser user, string password)
        {
            using var sqlConnection = new SqlConnection(configuration.GetConnectionString("EtlManagerContext"));
            using var sqlCommand = new SqlCommand(
                "EXEC [etlmanager].[UserAdd] @Username = @Username_, @Password = @Password_, @Role = @Role_, @Email = @Email_"
                , sqlConnection);
            sqlCommand.Parameters.AddWithValue("@Username_", user.Username);
            sqlCommand.Parameters.AddWithValue("@Password_", password);
            sqlCommand.Parameters.AddWithValue("@Role_", user.Role);
            if (user.Email is not null)
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
