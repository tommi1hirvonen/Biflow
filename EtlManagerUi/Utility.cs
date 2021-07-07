using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Pipes;
using System.Linq;
using System.Security.Cryptography;
using System.ServiceProcess;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using Dapper;
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

        public async static Task<Guid> StartExecutionAsync(IConfiguration configuration, Job job, string username, List<string>? stepIds = null, bool notify = false)
        {
            Guid executionId;
            using (var sqlConnection = new SqlConnection(configuration.GetConnectionString("EtlManagerContext")))
            {
                CommandDefinition command;
                var parameters = new DynamicParameters();
                parameters.AddDynamicParams(new { JobId_ = job.JobId, Username_ = username });
                if (stepIds is not null && stepIds.Count > 0)
                {
                    parameters.Add("StepIds_", string.Join(',', stepIds));
                    command = new CommandDefinition("EXEC [etlmanager].[ExecutionInitialize] @JobId = @JobId_, @Username = @Username_, @StepIds = @StepIds_",
                        parameters);
                }
                else
                {
                    command = new CommandDefinition("EXEC [etlmanager].[ExecutionInitialize] @JobId = @JobId_, @Username = @Username_",
                        parameters);
                }
                executionId = await sqlConnection.ExecuteScalarAsync<Guid>(command);
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
            using var sqlConnection = new SqlConnection(configuration.GetConnectionString("EtlManagerContext"));
            await sqlConnection.ExecuteAsync(
                @"UPDATE [etlmanager].[Job]
                SET [UseDependencyMode] = @Value
                WHERE [JobId] = @JobId", new { job.JobId, Value = enabled });
        }

        public async static Task ToggleJobEnabledAsync(IConfiguration configuration, Job job, bool enabled)
        {
            using var sqlConnection = new SqlConnection(configuration.GetConnectionString("EtlManagerContext"));
            await sqlConnection.ExecuteAsync(
                @"UPDATE [etlmanager].[Job]
                SET [IsEnabled] = @Value
                WHERE [JobId] = @JobId", new { job.JobId, Value = enabled });
        }

        public async static Task ToggleStepEnabledAsync(IConfiguration configuration, Step step, bool enabled)
        {
            using var sqlConnection = new SqlConnection(configuration.GetConnectionString("EtlManagerContext"));
            await sqlConnection.ExecuteAsync(
                @"UPDATE [etlmanager].[Step]
                SET [IsEnabled] = @Value
                WHERE [StepId] = @StepId", new { step.StepId, Value = enabled });
        }

        public async static Task RemoveTagAsync(IConfiguration configuration, Step step, Tag tag)
        {
            using var sqlConnection = new SqlConnection(configuration.GetConnectionString("EtlManagerContext"));
            await sqlConnection.ExecuteAsync(
                @"DELETE FROM [etlmanager].[StepTag]
                WHERE [StepsStepId] = @StepId AND [TagsTagId] = @TagId", new { step.StepId, tag.TagId });
        }

        public async static Task AddTagAsync(IConfiguration configuration, Step step, Tag tag)
        {
            using var sqlConnection = new SqlConnection(configuration.GetConnectionString("EtlManagerContext"));
            // Get id for existing tag
            var guid = await sqlConnection.ExecuteScalarAsync<Guid?>(
                "SELECT TagId FROM etlmanager.Tag WHERE TagName = @TagName", new { tag.TagName });
            // No tag found => insert a new tag.
            if (guid is null)
            {
                guid = Guid.NewGuid();
                await sqlConnection.ExecuteAsync(
                    "INSERT INTO etlmanager.Tag (TagId, TagName) SELECT @TagId, @TagName", new { TagId = guid, tag.TagName });
            }

            tag.TagId = (Guid)guid;

            // Insert a link between the step and tag.
            await sqlConnection.ExecuteAsync(
                @"INSERT INTO etlmanager.StepTag (StepsStepId, TagsTagId)
                SELECT @StepId, @TagId
                WHERE NOT EXISTS (SELECT * FROM etlmanager.StepTag WHERE StepsStepId = @StepId AND TagsTagId = @TagId)",
                new { step.StepId, tag.TagId });
        }

        public static (bool Running, bool Error, string Status) SchedulerServiceGetStatus()
        {
            try
            {
#pragma warning disable CA1416 // Validate platform compatibility
                var serviceController = new ServiceController("EtlManagerScheduler");
                var status = serviceController.Status switch
                {
                    ServiceControllerStatus.Running => "Running",
                    ServiceControllerStatus.Stopped => "Stopped",
                    ServiceControllerStatus.Paused => "Paused",
                    ServiceControllerStatus.StopPending => "Stopping",
                    ServiceControllerStatus.StartPending => "Starting",
                    ServiceControllerStatus.ContinuePending => "Continue pending",
                    ServiceControllerStatus.PausePending => "Pause pending",
                    _ => "Unknown"
                };
                return (status == "Running", false, status);
#pragma warning restore CA1416 // Validate platform compatibility
            }
            catch (Exception)
            {
                return (false, true, "Unknown");
            }
        }

        private static JsonSerializerOptions SchedulerCommandSerializerOptions() =>
            new() { Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) } };

        public async static Task<bool> SchedulerServiceDeleteJobAsync(Job job)
        {
            // If the scheduler service is not running, return true.
            // This way the changes can be committed to the database.
            (var running, var _, var _) = SchedulerServiceGetStatus();

            if (!running)
                return true;

            // Connect to the pipe server set up by the scheduler service.
            using var pipeClient = new NamedPipeClientStream(".", "ETL Manager Scheduler", PipeDirection.InOut); // "." => the pipe server is on the same computer
            await pipeClient.ConnectAsync(10000); // wait for 10 seconds
#pragma warning disable CA1416 // Validate platform compatibility
            pipeClient.ReadMode = PipeTransmissionMode.Message; // Each byte array is transferred as a single message
#pragma warning restore CA1416 // Validate platform compatibility

            // Send delete command.
            var addCommand = new SchedulerCommand(SchedulerCommand.CommandType.Delete, job.JobId.ToString(), null, null);
            var json = JsonSerializer.Serialize(addCommand, SchedulerCommandSerializerOptions());
            var bytes = Encoding.UTF8.GetBytes(json);
            pipeClient.Write(bytes, 0, bytes.Length);


            // Get response from scheduler service
            var responseBytes = CommonUtility.ReadMessage(pipeClient);
            var response = Encoding.UTF8.GetString(responseBytes);
            return response == "SUCCESS";
        }

        public async static Task<bool> SchedulerServiceSynchronize()
        {
            // Connect to the pipe server set up by the scheduler service.
            using var pipeClient = new NamedPipeClientStream(".", "ETL Manager Scheduler", PipeDirection.InOut); // "." => the pipe server is on the same computer
            await pipeClient.ConnectAsync(10000); // wait for 10 seconds
#pragma warning disable CA1416 // Validate platform compatibility
            pipeClient.ReadMode = PipeTransmissionMode.Message; // Each byte array is transferred as a single message
#pragma warning restore CA1416 // Validate platform compatibility

            // Send synchronize command
            var command = new SchedulerCommand(SchedulerCommand.CommandType.Synchronize, null, null, null);
            var json = JsonSerializer.Serialize(command, SchedulerCommandSerializerOptions());
            var bytes = Encoding.UTF8.GetBytes(json);
            pipeClient.Write(bytes, 0, bytes.Length);

            // Get response from the scheduler service.
            var responseBytes = CommonUtility.ReadMessage(pipeClient);
            var response = Encoding.UTF8.GetString(responseBytes);
            return response == "SUCCESS";
        }

        public async static Task<bool> SchedulerServiceSendCommandAsync(SchedulerCommand.CommandType commandType, Schedule? schedule)
        {
            // If the scheduler service is not running, return true.
            // This way the changes can be committed to the database.
            (var running, var _, var _) = SchedulerServiceGetStatus();

            if (!running)
                return true;

            // Connect to the pipe server set up by the scheduler service.
            using var pipeClient = new NamedPipeClientStream(".", "ETL Manager Scheduler", PipeDirection.InOut); // "." => the pipe server is on the same computer
            await pipeClient.ConnectAsync(10000); // wait for 10 seconds
#pragma warning disable CA1416 // Validate platform compatibility
            pipeClient.ReadMode = PipeTransmissionMode.Message; // Each byte array is transferred as a single message
#pragma warning restore CA1416 // Validate platform compatibility

            // Send add command.
            var addCommand = new SchedulerCommand(commandType, schedule?.JobId.ToString(), schedule?.ScheduleId.ToString(), schedule?.CronExpression);
            var json = JsonSerializer.Serialize(addCommand, SchedulerCommandSerializerOptions());
            var bytes = Encoding.UTF8.GetBytes(json);
            pipeClient.Write(bytes, 0, bytes.Length);


            // Get response from scheduler service
            var responseBytes = CommonUtility.ReadMessage(pipeClient);
            var response = Encoding.UTF8.GetString(responseBytes);
            return response == "SUCCESS";
        }

        public async static Task<bool> ToggleScheduleEnabledAsync(IConfiguration configuration, Schedule schedule, bool enabled)
        {
            using var sqlConnection = new SqlConnection(configuration.GetConnectionString("EtlManagerContext"));  
            await sqlConnection.OpenAsync();
            using var transaction = sqlConnection.BeginTransaction();
            await sqlConnection.ExecuteAsync(
                @"UPDATE [etlmanager].[Schedule]
                SET [IsEnabled] = @Value
                WHERE [ScheduleId] = @ScheduleId", new { schedule.ScheduleId, Value = enabled }, transaction);
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
            var createdJobId = await sqlConnection.ExecuteScalarAsync<Guid>(
                "EXEC [etlmanager].[JobCopy] @JobId = @JobId_, @Username = @Username_",
                new { JobId_ = jobId, Username_ = username });
            return createdJobId;
        }

        public async static Task<Guid> StepCopyAsync(IConfiguration configuration, Guid stepId, Guid targetJobId, string username)
        {
            using var sqlConnection = new SqlConnection(configuration.GetConnectionString("EtlManagerContext"));
            var createdStepId = await sqlConnection.ExecuteScalarAsync<Guid>(
                "EXEC [etlmanager].[StepCopy] @StepId = @StepId_, @TargetJobId = @TargetJobId_, @Username = @Username_",
                new { StepId_ = stepId, TargetJobId_ = targetJobId, Username_ = username });
            return createdStepId;
        }

        public static async Task<bool> IsEncryptionKeySetAsync(IConfiguration configuration)
        {
            string connectionString = configuration.GetConnectionString("EtlManagerContext");
            string encryptionId = configuration.GetValue<string>("EncryptionId");
            using var sqlConnection = new SqlConnection(connectionString);
            var count = await sqlConnection.ExecuteScalarAsync<int>(
                "SELECT COUNT(*) FROM etlmanager.EncryptionKey WHERE EncryptionId = @EncryptionId",
                new { EncryptionId = encryptionId });
            return count > 0;
        }

        public static AuthenticationResult AuthenticateUser(IConfiguration configuration, string username, string password)
        {
            using var sqlConnection = new SqlConnection(configuration.GetConnectionString("EtlManagerContext"));
            var role = sqlConnection.ExecuteScalar<string?>(
                "EXEC [etlmanager].[UserAuthenticate] @Username = @Username_, @Password = @Password_",
                new { Username_ = username, Password_ = password });
            return new AuthenticationResult(role);
        }

        public static async Task<Dictionary<string, Dictionary<string, List<string>>>> GetSSISCatalogPackages(IConfiguration configuration, string connectionId)
        {
            var encryptionKey = await CommonUtility.GetEncryptionKeyAsync(configuration);
            string catalogConnectionString;
            using (var sqlConnection = new SqlConnection(configuration.GetConnectionString("EtlManagerContext")))
            {
                catalogConnectionString = await sqlConnection.ExecuteScalarAsync<string>(
                    "SELECT etlmanager.GetConnectionStringDecrypted(@ConnectionId, @EncryptionKey) AS ConnectionString",
                    new { EncryptionKey = encryptionKey, ConnectionId = connectionId });
            }
            using (var sqlConnection = new SqlConnection(catalogConnectionString))
            {
                var rows = await sqlConnection.QueryAsync<(string, string?, string?)>(
                    @"SELECT
	                    [folders].[name] AS FolderName,
	                    [projects].[name] AS ProjectName,
	                    [packages].[name] AS PackageName
                    FROM [SSISDB].[catalog].[folders]
	                    LEFT JOIN [SSISDB].[catalog].[projects] ON [folders].[folder_id] = [projects].[folder_id]
	                    LEFT JOIN [SSISDB].[catalog].[packages] ON [projects].[project_id] = [packages].[project_id]
                    ORDER BY FolderName, ProjectName, PackageName");
                var catalog = rows
                    .GroupBy(key => key.Item1, element => (element.Item2, element.Item3))
                    .ToDictionary(
                    grouping => grouping.Key,
                    grouping => grouping
                                    .Where(x => x.Item1 is not null)
                                    .GroupBy(key => key.Item1, element => element.Item2)
                                    .ToDictionary(
                                        grouping_ => grouping_.Key ?? "",
                                        grouping_ => grouping_.Where(x => x is not null).Select(x => x ?? "").ToList()));
                return catalog;
            }
        }

        public static async Task<Dictionary<string, List<string>>> GetDatabaseStoredProcedures(IConfiguration configuration, string connectionId)
        {
            var encryptionKey = await CommonUtility.GetEncryptionKeyAsync(configuration);
            string connectionString;
            using (var sqlConnection = new SqlConnection(configuration.GetConnectionString("EtlManagerContext")))
            {
                connectionString = await sqlConnection.ExecuteScalarAsync<string>(
                    "SELECT etlmanager.GetConnectionStringDecrypted(@ConnectionId, @EncryptionKey) AS ConnectionString",
                    new { EncryptionKey = encryptionKey, ConnectionId = connectionId });
            }
            using (var sqlConnection = new SqlConnection(connectionString))
            {
                var rows = await sqlConnection.QueryAsync<(string, string)>(
                    @"SELECT OBJECT_SCHEMA_NAME([object_id]) AS [schema], [name]
                    FROM [sys].[procedures]
                    ORDER BY [schema], [name]");
                var procedures = rows
                    .GroupBy(key => key.Item1, element => element.Item2)
                    .ToDictionary(grouping => grouping.Key, grouping => grouping.ToList());
                return procedures;
            }
        }

        public static async Task<bool> UpdatePasswordAsync(IConfiguration configuration, string username, string password)
        {
            using var sqlConnection = new SqlConnection(configuration.GetConnectionString("EtlManagerContext"));
            var result = await sqlConnection.ExecuteScalarAsync<int>(
                "EXEC [etlmanager].[UserUpdatePassword] @Username = @Username_, @Password = @Password_",
                new { Username_ = username, Password_ = password });

            if (result > 0) return true;
            else return false;
        }

        public static async Task<bool> AddUserAsync(IConfiguration configuration, RoleUser user, string password)
        {
            using var sqlConnection = new SqlConnection(configuration.GetConnectionString("EtlManagerContext"));
            var result = await sqlConnection.ExecuteScalarAsync<int>(
                "EXEC [etlmanager].[UserAdd] @Username = @Username_, @Password = @Password_, @Role = @Role_, @Email = @Email_",
                new { Username_ = user.Username, Password_ = password, Role_ = user.Role, Email_ = user.Email });

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
        public string? Role { get; }
        public AuthenticationResult(string? role)
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
