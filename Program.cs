using System;
using System.IO;
using System.Threading.Tasks;
using EtlManager.Models;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Data.SqlClient;
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

        public async static Task<Guid> StartExecution(IConfiguration configuration, Job job)
        {
            using SqlConnection sqlConnection = new SqlConnection(configuration.GetConnectionString("EtlManagerContext"));
            SqlCommand sqlCommand = new SqlCommand(
                "EXEC [etlmanager].[JobExecute] @JobId = @JobId_"
                , sqlConnection);

            sqlCommand.Parameters.AddWithValue("@JobId_", job.JobId.ToString());

            await sqlConnection.OpenAsync();
            Guid executionId = (Guid) await sqlCommand.ExecuteScalarAsync();
            return executionId;
        }

        public async static Task ToggleStepEnabled(IConfiguration configuration, Step step)
        {
            using SqlConnection sqlConnection = new SqlConnection(configuration.GetConnectionString("EtlManagerContext"));
            SqlCommand sqlCommand = new SqlCommand(
                "UPDATE [etlmanager].[Step]\n" +
                "SET [IsEnabled] = CASE [IsEnabled] WHEN 1 THEN 0 ELSE 1 END\n" +
                "WHERE [StepId] = @StepId"

                , sqlConnection);
            sqlCommand.Parameters.AddWithValue("@StepId", step.StepId.ToString());

            await sqlConnection.OpenAsync();
            await sqlCommand.ExecuteNonQueryAsync();
        }

        public async static Task JobCopy(IConfiguration configuration, Guid jobId)
        {
            using SqlConnection sqlConnection = new SqlConnection(configuration.GetConnectionString("EtlManagerContext"));
            SqlCommand sqlCommand = new SqlCommand(
                "EXEC [etlmanager].[JobCopy] @JobId = @JobId_"
                , sqlConnection);
            sqlCommand.Parameters.AddWithValue("@JobId_", jobId.ToString());

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
    }
    
}
