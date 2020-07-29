using System;
using System.Threading.Tasks;
using ExecutorManager.Models;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace ExecutorManager
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
                    webBuilder.UseStartup<Startup>();
                });

    }

    public static class Utility
    {
        public async static Task StartExecution(IConfiguration configuration, Job job)
        {
            SqlConnection sqlConnection = new SqlConnection(configuration.GetConnectionString("ExecutorManagerContext"));
            SqlCommand sqlCommand = new SqlCommand(
                "DECLARE @execution_id BIGINT\n" +

                "EXEC[SSISDB].[catalog].[create_execution]\n" +
                    "@package_name = @PackageName,\n" +
                    "@execution_id = @execution_id OUTPUT,\n" +
                    "@folder_name = @FolderName,\n" +
                    "@project_name = @ProjectName,\n" +
                    "@use32bitruntime = 0,\n" +
                    "@reference_id = NULL\n" +

                "EXEC[SSISDB].[catalog].[set_execution_parameter_value]\n" +
                    "@execution_id,\n" +
                    "@object_type = 50,\n" +
                    "@parameter_name = N'LOGGING_LEVEL',\n" +
                    "@parameter_value = 1\n" +

                "EXEC[SSISDB].[catalog].[set_execution_parameter_value]\n" +
                    "@execution_id,\n" +
                    "@object_type = 50,\n" +
                    "@parameter_name = N'SYNCHRONIZED',\n" +
                    "@parameter_value = 0\n" +

                "EXEC[SSISDB].[catalog].[set_execution_parameter_value]\n" +
                    "@execution_id,\n" +
                    "@object_type = 30,\n" +
                    "@parameter_name = N'JobId',\n" +
                    "@parameter_value = @JobId\n" +

                "EXEC[SSISDB].[catalog].[start_execution]\n" +
                    "@execution_id"

                , sqlConnection);
            sqlCommand.Parameters.AddWithValue("@PackageName", "MasterExecutor.dtsx");
            sqlCommand.Parameters.AddWithValue("@FolderName", "Executor");
            sqlCommand.Parameters.AddWithValue("@ProjectName", "Executor");
            sqlCommand.Parameters.AddWithValue("@JobId", job.JobId.ToString());

            await sqlConnection.OpenAsync();
            await sqlCommand.ExecuteNonQueryAsync();
            await sqlConnection.CloseAsync();
        }

        public async static Task ToggleStepDisabled(IConfiguration configuration, Step step)
        {
            SqlConnection sqlConnection = new SqlConnection(configuration.GetConnectionString("ExecutorManagerContext"));
            SqlCommand sqlCommand = new SqlCommand(
                "UPDATE [Executor].[executor].[Step]\n" +
                "SET [IsDisabled] = CASE [IsDisabled] WHEN 1 THEN 0 ELSE 1 END\n" +
                "WHERE [StepId] = @StepId"

                , sqlConnection);
            sqlCommand.Parameters.AddWithValue("@StepId", step.StepId.ToString());

            await sqlConnection.OpenAsync();
            await sqlCommand.ExecuteNonQueryAsync();
            await sqlConnection.CloseAsync();
        }

        public static bool AuthenticateUser(IConfiguration configuration, string username, string password)
        {
            SqlConnection sqlConnection = new SqlConnection(configuration.GetConnectionString("ExecutorManagerContext"));
            SqlCommand sqlCommand = new SqlCommand(
                "EXEC [Executor].[executor].[UserAuthenticate] @Username = @Username_, @Password = @Password_"
                , sqlConnection);
            sqlCommand.Parameters.AddWithValue("@Username_", username);
            sqlCommand.Parameters.AddWithValue("@Password_", password);

            sqlConnection.Open();
            int result = (int)sqlCommand.ExecuteScalar();
            sqlConnection.Close();

            if (result > 0) return true;
            else return false;
        }

        public static bool UpdatePassword(IConfiguration configuration, string username, string password)
        {
            SqlConnection sqlConnection = new SqlConnection(configuration.GetConnectionString("ExecutorManagerContext"));
            SqlCommand sqlCommand = new SqlCommand(
                "EXEC [Executor].[executor].[UserUpdatePassword] @Username = @Username_, @Password = @Password_"
                , sqlConnection);
            sqlCommand.Parameters.AddWithValue("@Username_", username);
            sqlCommand.Parameters.AddWithValue("@Password_", password);

            sqlConnection.Open();
            int result = (int)sqlCommand.ExecuteScalar();
            sqlConnection.Close();

            if (result > 0) return true;
            else return false;
        }
    }
    
}
