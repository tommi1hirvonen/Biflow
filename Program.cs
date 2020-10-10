using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using EtlManager.Models;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion.Internal;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;

namespace EtlManager
{
    public class Program
    {
        public static void Main(string[] args)
        {
            CreateHostBuilder(args).Build().Run();
        }

        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .ConfigureWebHostDefaults(webBuilder =>
                {
                    webBuilder
                    .UseIISIntegration()
                    .UseContentRoot(Directory.GetCurrentDirectory())
                    .UseStartup<Startup>();
                });

    }

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

        public async static Task<Guid> StartExecution(IConfiguration configuration, Job job, string username, List<string> stepIds = null)
        {
            using SqlConnection sqlConnection = new SqlConnection(configuration.GetConnectionString("EtlManagerContext"));
            SqlCommand sqlCommand;
            
            if (stepIds != null && stepIds.Count > 0)
            {
                sqlCommand = new SqlCommand(
                "EXEC [etlmanager].[JobExecute] @JobId = @JobId_, @Username = @Username_, @StepIds = @StepIds_"
                , sqlConnection);

                sqlCommand.Parameters.AddWithValue("@StepIds_", string.Join(',', stepIds));
            }
            else
            {
                sqlCommand = new SqlCommand(
                "EXEC [etlmanager].[JobExecute] @JobId = @JobId_, @Username = @Username_"
                , sqlConnection);
            }

            sqlCommand.Parameters.AddWithValue("@JobId_", job.JobId.ToString());
            sqlCommand.Parameters.AddWithValue("@Username_", username);

            await sqlConnection.OpenAsync();
            Guid executionId = (Guid) await sqlCommand.ExecuteScalarAsync();
            return executionId;
        }

        public async static Task StopJobExecution(IConfiguration configuration, Guid executionId)
        {
            List<Task> tasks = new List<Task>();

            using SqlConnection sqlConnection = new SqlConnection(configuration.GetConnectionString("EtlManagerContext"));
            await sqlConnection.OpenAsync();

            // First stop the MasterExecutor operation.
            SqlCommand fetchMasterOperationId = new SqlCommand(
                "SELECT TOP 1 MasterExecutorOperationId FROM etlmanager.Execution WHERE ExecutionId = @ExecutionId"
                , sqlConnection);
            fetchMasterOperationId.Parameters.AddWithValue("@ExecutionId", executionId);
            long masterOperationId = (long)await fetchMasterOperationId.ExecuteScalarAsync();
            tasks.Add(StopPackage(sqlConnection, masterOperationId));

            // Fetch all slave operation ids and iterate over them stopping each slave execution.
            SqlCommand fetchSlaveOperationIds = new SqlCommand(
                @"SELECT SlaveExecutorOperationId, PackageOperationId, ServerName
                FROM etlmanager.Execution
                WHERE ExecutionId = @ExecutionId AND EndDateTime IS NULL AND SlaveExecutorOperationId IS NOT NULL
                ORDER BY RetryAttemptIndex DESC"
                , sqlConnection);
            fetchSlaveOperationIds.Parameters.AddWithValue("@ExecutionId", executionId);
            SqlDataReader slaveOperationReader = await fetchSlaveOperationIds.ExecuteReaderAsync();
            while (slaveOperationReader.Read())
            {
                long slaveOperationId = (long)slaveOperationReader[0];
                long packageOperationId = 0;
                string packageServerName = null;
                if (!slaveOperationReader.IsDBNull(1)) packageOperationId = (long)slaveOperationReader[1];
                if (!slaveOperationReader.IsDBNull(2)) packageServerName = (string)slaveOperationReader[2];

                tasks.Add(StopSlaveExecution(sqlConnection, slaveOperationId, packageServerName, packageOperationId));
            }
            await slaveOperationReader.CloseAsync();

            // Wait for all stop commands to finish.
            await Task.WhenAll(tasks);

            SqlCommand updateStatuses = new SqlCommand(
              @"UPDATE etlmanager.Execution
                SET EndDateTime = GETDATE(),
	                ExecutionStatus = 'STOPPED'
                WHERE ExecutionId = @ExecutionId AND EndDateTime IS NULL AND StartDateTime IS NOT NULL"
                , sqlConnection);
            updateStatuses.Parameters.AddWithValue("@ExecutionId", executionId);
            await updateStatuses.ExecuteNonQueryAsync();
        }

        public async static Task StopStepExecution(IConfiguration configuration, Guid executionId, Guid stepId, int retryAttemptIndex)
        {
            using SqlConnection sqlConnection = new SqlConnection(configuration.GetConnectionString("EtlManagerContext"));

            SqlCommand fetchOperation = new SqlCommand(
              @"SELECT TOP 1 SlaveExecutorOperationId, PackageOperationId, ServerName
                FROM etlmanager.Execution
                WHERE ExecutionId = @ExecutionId AND StepId = @StepId AND RetryAttemptIndex = @RetryAttemptIndex"
                , sqlConnection);
            fetchOperation.Parameters.AddWithValue("@ExecutionId", executionId);
            fetchOperation.Parameters.AddWithValue("@StepId", stepId);
            fetchOperation.Parameters.AddWithValue("@RetryAttemptIndex", retryAttemptIndex);

            await sqlConnection.OpenAsync();

            SqlDataReader reader = await fetchOperation.ExecuteReaderAsync();

            if (reader.HasRows && reader.Read())
            {
                long slaveOperationId = (long)reader[0];
                long packageOperationId = 0;
                string packageServerName = null;
                if (!reader.IsDBNull(1)) packageOperationId = (long)reader[1];
                if (!reader.IsDBNull(2)) packageServerName = (string)reader[2];

                await StopSlaveExecution(sqlConnection, slaveOperationId, packageServerName, packageOperationId);
            }

            SqlCommand updateStatus = new SqlCommand(
              @"UPDATE etlmanager.Execution
                SET EndDateTime = GETDATE(),
	                ExecutionStatus = 'STOPPED'
                WHERE ExecutionId = @ExecutionId AND StepId = @StepId AND RetryAttemptIndex = @RetryAttemptIndex
                    AND EndDateTime IS NULL AND StartDateTime IS NOT NULL"
                , sqlConnection);
            updateStatus.Parameters.AddWithValue("@ExecutionId", executionId);
            updateStatus.Parameters.AddWithValue("@StepId", stepId);
            updateStatus.Parameters.AddWithValue("@RetryAttemptIndex", retryAttemptIndex);
            await updateStatus.ExecuteNonQueryAsync();
        }

        private async static Task StopSlaveExecution(SqlConnection sqlConnection, long slaveOperationId, string childPackageServerName, long childPackageOperationId)
        {
            // First stop the SlaveExecutor operation.
            await StopPackage(sqlConnection, slaveOperationId);

            // If it is an SSIS step, also stop the child package operation.
            if (childPackageOperationId > 0)
            {
                await StopPackage(childPackageServerName, childPackageOperationId);
            }
        }

        private async static Task StopPackage(string serverName, long operationId)
        {
            using SqlConnection sqlConnection = new SqlConnection("Data Source=" + serverName + ";Initial Catalog=SSISDB;Integrated Security=SSPI;");
            await sqlConnection.OpenAsync();
            await StopPackage(sqlConnection, operationId);
        }

        private async static Task StopPackage(SqlConnection sqlConnection, long operationId)
        {
            SqlCommand stopPackageOperationCmd = new SqlCommand("EXEC SSISDB.catalog.stop_operation @OperationId", sqlConnection) { CommandTimeout = 60 }; // One minute
            stopPackageOperationCmd.Parameters.AddWithValue("@OperationId", operationId);
            await stopPackageOperationCmd.ExecuteNonQueryAsync();
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

        public static AuthenticationResult AuthenticateUser(IConfiguration configuration, string username, string password)
        {
            using SqlConnection sqlConnection = new SqlConnection(configuration.GetConnectionString("EtlManagerContext"));
            SqlCommand sqlCommand = new SqlCommand(
                "EXEC [etlmanager].[UserAuthenticate] @Username = @Username_, @Password = @Password_"
                , sqlConnection);
            sqlCommand.Parameters.AddWithValue("@Username_", username);
            sqlCommand.Parameters.AddWithValue("@Password_", password);

            sqlConnection.Open();
            string role = (string)sqlCommand.ExecuteScalar();

            return new AuthenticationResult(role);
        }

        public static bool UpdatePassword(IConfiguration configuration, string username, string password)
        {
            using SqlConnection sqlConnection = new SqlConnection(configuration.GetConnectionString("EtlManagerContext"));
            SqlCommand sqlCommand = new SqlCommand(
                "EXEC [etlmanager].[UserUpdatePassword] @Username = @Username_, @Password = @Password_"
                , sqlConnection);
            sqlCommand.Parameters.AddWithValue("@Username_", username);
            sqlCommand.Parameters.AddWithValue("@Password_", password);

            sqlConnection.Open();
            int result = (int)sqlCommand.ExecuteScalar();

            if (result > 0) return true;
            else return false;
        }

        public static bool AddUser(IConfiguration configuration, RoleUser user, string password)
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
            

            sqlConnection.Open();
            int result = (int)sqlCommand.ExecuteScalar();

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
