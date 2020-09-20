using System;
using System.Collections.Generic;
using System.IO;
using System.Numerics;
using System.Threading.Tasks;
using EtlManager.Models;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore.ValueGeneration.Internal;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

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
            using SqlConnection sqlConnection = new SqlConnection(configuration.GetConnectionString("EtlManagerContext"));

            SqlCommand fetchOperationId = new SqlCommand(
                "SELECT TOP 1 MasterExecutorOperationId FROM etlmanager.Execution WHERE ExecutionId = @ExecutionId"
                , sqlConnection);
            fetchOperationId.Parameters.AddWithValue("@ExecutionId", executionId);

            await sqlConnection.OpenAsync();

            long operationId = (long)await fetchOperationId.ExecuteScalarAsync();

            SqlCommand stopOperation = new SqlCommand("EXEC SSISDB.catalog.stop_operation @OperationId", sqlConnection);
            stopOperation.Parameters.AddWithValue("@OperationId", operationId);
            await stopOperation.ExecuteNonQueryAsync();

            SqlCommand updateStatuses = new SqlCommand(
              @"UPDATE etlmanager.Execution
                SET EndDateTime = CASE WHEN StartDateTime IS NOT NULL THEN GETDATE() ELSE EndDateTime END,
	                ExecutionStatus = 'STOPPED'
                WHERE ExecutionId = @ExecutionId AND EndDateTime IS NULL"
                , sqlConnection);
            updateStatuses.Parameters.AddWithValue("@ExecutionId", executionId);
            await updateStatuses.ExecuteNonQueryAsync();
            
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

        public static bool AuthenticateUser(IConfiguration configuration, string username, string password)
        {
            using SqlConnection sqlConnection = new SqlConnection(configuration.GetConnectionString("EtlManagerContext"));
            SqlCommand sqlCommand = new SqlCommand(
                "EXEC [etlmanager].[UserAuthenticate] @Username = @Username_, @Password = @Password_"
                , sqlConnection);
            sqlCommand.Parameters.AddWithValue("@Username_", username);
            sqlCommand.Parameters.AddWithValue("@Password_", password);

            sqlConnection.Open();
            int result = (int)sqlCommand.ExecuteScalar();

            if (result > 0) return true;
            else return false;
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

        public static DateTime StartOfWeek(this DateTime dt, DayOfWeek startOfWeek)
        {
            int diff = (7 + (dt.DayOfWeek - startOfWeek)) % 7;
            return dt.AddDays(-1 * diff).Date;
        }
    }
    
}
