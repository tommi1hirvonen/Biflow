using Serilog;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Text;
using System.Threading;

namespace EtlManagerExecutor
{
    class PackageExecution
    {
        private readonly int pollingIntervalMs;
        public string ConnectionString { get; set; }
        public string FolderName { get; set; }
        public string ProjectName { get; set; }
        public string PackageName { get; set; }
        public bool ExecuteIn32BitMode { get; set; } = false;
        public Dictionary<string, string> Parameters { get; set; } = new Dictionary<string, string>();
        public long OperationId { get; set; }
        public bool Completed { get; set; } = false;
        public bool Success { get; set; } = false;

        private const int MaxRefreshRetries = 5;

        public PackageExecution(string connectionString, string folderName, string projectName, string packageName, bool executeIn32BitMode, int pollingIntervalMs)
        {
            ConnectionString = connectionString;
            FolderName = folderName;
            ProjectName = projectName;
            PackageName = packageName;
            ExecuteIn32BitMode = executeIn32BitMode;
            this.pollingIntervalMs = pollingIntervalMs;
        }

        public long StartExecution()
        {
            using SqlConnection sqlConnection = new SqlConnection(ConnectionString);
            StringBuilder commandBuilder = new StringBuilder();
            commandBuilder.Append(
                @"DECLARE @execution_id BIGINT

                EXEC [SSISDB].[catalog].[create_execution]
                    @package_name = @PackageName,
                    @execution_id = @execution_id OUTPUT,
                    @folder_name = @FolderName,
                    @project_name = @ProjectName,
                    @use32bitruntime = @ExecuteIn32BitMode,
                    @reference_id = NULL

                EXEC [SSISDB].[catalog].[set_execution_parameter_value]
                    @execution_id,
                    @object_type = 50,
                    @parameter_name = N'LOGGING_LEVEL',
                    @parameter_value = 1

                EXEC [SSISDB].[catalog].[set_execution_parameter_value]
                    @execution_id,
                    @object_type = 50,
                    @parameter_name = N'SYNCHRONIZED',
                    @parameter_value = 0" + "\n"
                );

            foreach (var parameter in Parameters)
            {
                commandBuilder.Append(
                    @"EXEC [SSISDB].[catalog].[set_execution_parameter_value]
                        @execution_id,
                        @object_type = 30,
                        @parameter_name = @ParameterName" + parameter.Key + @",
                        @parameter_value = @ParameterValue" + parameter.Key + "\n"
                    );
            }

            commandBuilder.Append(
                @"EXEC [SSISDB].[catalog].[start_execution] @execution_id

                SELECT @execution_id"
                );
            string commandString = commandBuilder.ToString();
            SqlCommand executionCommand = new SqlCommand(commandString, sqlConnection);

            executionCommand.Parameters.AddWithValue("@FolderName", FolderName);
            executionCommand.Parameters.AddWithValue("@ProjectName", ProjectName);
            executionCommand.Parameters.AddWithValue("@PackageName", PackageName);
            executionCommand.Parameters.AddWithValue("@ExecuteIn32BitMode", ExecuteIn32BitMode ? 1 : 0);

            foreach (var parameter in Parameters)
            {
                executionCommand.Parameters.AddWithValue("@ParameterName" + parameter.Key, parameter.Key);
                executionCommand.Parameters.AddWithValue("@ParameterValue" + parameter.Key, parameter.Value);
            }

            sqlConnection.Open();
            OperationId = (long)executionCommand.ExecuteScalar();
            return OperationId;
        }

        public void TryRefreshStatus()
        {
            int refreshRetries = 0;
            // Try to refresh the operation status until the maximum number of attempts is reached.
            while (refreshRetries < MaxRefreshRetries)
            {
                try
                {
                    using SqlConnection sqlConnection = new SqlConnection(ConnectionString);
                    SqlCommand sqlCommand = new SqlCommand("SELECT status from SSISDB.catalog.operations where operation_id = @OperationId", sqlConnection);
                    sqlCommand.Parameters.AddWithValue("@OperationId", OperationId);
                    sqlConnection.Open();
                    int status = (int)sqlCommand.ExecuteScalar();
                    // created (1), running (2), canceled (3), failed (4), pending (5), ended unexpectedly (6), succeeded (7), stopping (8), completed (9)
                    if (status == 3 || status == 4 || status == 6 || status == 7 || status == 9)
                    {
                        Completed = true;
                        if (status == 7) Success = true;
                    }

                    return;
                }
                catch (Exception ex)
                {
                    Log.Error(ex, "Error refreshing package operation status for operation id {operationId}", OperationId);
                    refreshRetries++;
                    Thread.Sleep(pollingIntervalMs);
                }
            }
            // The maximum number of attempts was reached. Notify caller with exception.
            throw new TimeoutException("The maximum number of package operation status refresh attempts was reached.");
        }

        public List<string> GetErrorMessages()
        {
            using SqlConnection sqlConnection = new SqlConnection(ConnectionString);
            SqlCommand sqlCommand = new SqlCommand(
                @"SELECT message
                FROM SSISDB.catalog.operation_messages
                WHERE message_type = 120 AND operation_id = @OperationId" // message_type = 120 => error message
                , sqlConnection);
            sqlCommand.Parameters.AddWithValue("@OperationId", OperationId);
            sqlConnection.Open();
            List<string> messages = new List<string>();
            var reader = sqlCommand.ExecuteReader();
            while (reader.Read())
            {
                messages.Add(reader[0].ToString());
            }
            return messages;
        }
    }
}
