using Serilog;
using System;
using System.Data.SqlClient;
using System.Diagnostics;
using System.IO;

namespace EtlManagerExecutor
{
    class JobStepExecution : IStepExecution
    {
        private readonly ExecutionConfiguration executionConfig;
        private readonly JobStepConfiguration jobStep;

        public JobStepExecution(ExecutionConfiguration executionConfiguration, JobStepConfiguration jobStepConfiguration)
        {
            this.executionConfig = executionConfiguration;
            jobStep = jobStepConfiguration;
        }

        public ExecutionResult Run()
        {

            Process executorProcess;
            string executionId;

            using (SqlConnection sqlConnection = new SqlConnection(executionConfig.ConnectionString))
            {
                sqlConnection.Open();

                SqlCommand initCommand = new SqlCommand(
                        "EXEC etlmanager.ExecutionInitialize @JobId = @JobId_"
                        , sqlConnection);
                initCommand.Parameters.AddWithValue("@JobId_", jobStep.JobToExecuteId);

                try
                {
                    executionId = initCommand.ExecuteScalar().ToString();
                }
                catch (Exception ex)
                {
                    Log.Error(ex, "{ExecutionId} {StepId} Error initializing execution for job {jobId}", executionConfig.ExecutionId, jobStep.StepId, jobStep.JobToExecuteId);
                    return new ExecutionResult.Failure("Error initializing job execution: " + ex.Message);
                }

                string executorFilePath = Process.GetCurrentProcess().MainModule.FileName;
                ProcessStartInfo executionInfo = new ProcessStartInfo()
                {
                    FileName = executorFilePath,
                    ArgumentList = {
                        "execute",
                        "--id",
                        executionId.ToString(),
                        executionConfig.Notify ? "--notify" : ""
                    },
                    // Set WorkingDirectory for the EtlManagerExecutor executable.
                    // This way it reads the configuration file (appsettings.json) from the correct folder.
                    WorkingDirectory = Path.GetDirectoryName(executorFilePath),
                    UseShellExecute = true,
                    CreateNoWindow = true,
                    WindowStyle = ProcessWindowStyle.Hidden
                };

                executorProcess = new Process() { StartInfo = executionInfo };
                try
                {
                    executorProcess.Start();
                }
                catch (Exception ex)
                {
                    Log.Error(ex, "{ExecutionId} {StepId} Error starting executor process for execution {executionId}", executionConfig.ExecutionId, jobStep.StepId, executionId);
                    return new ExecutionResult.Failure("Error starting executor process: " + ex.Message);
                }

                SqlCommand processIdCmd = new SqlCommand(
                "UPDATE etlmanager.Execution SET ExecutorProcessId = @ProcessId WHERE ExecutionId = @ExecutionId", sqlConnection);
                processIdCmd.Parameters.AddWithValue("@ProcessId", executorProcess.Id);
                processIdCmd.Parameters.AddWithValue("@ExecutionId", executionId);

                try
                {
                    processIdCmd.ExecuteNonQuery();
                }
                catch (Exception ex)
                {
                    Log.Error(ex, "{ExecutionId} {StepId} Error updating executor process id for execution {executionId}", executionConfig.ExecutionId, jobStep.StepId, executionId);
                }

            }

            if (jobStep.JobExecuteSynchronized)
            {
                executorProcess.WaitForExit();
                try
                {
                    using SqlConnection sqlConnection = new SqlConnection(executionConfig.ConnectionString);
                    sqlConnection.Open();
                    SqlCommand sqlCommand = new SqlCommand("SELECT TOP 1 ExecutionStatus FROM etlmanager.vExecutionJob WHERE ExecutionId = @ExecutionId", sqlConnection);
                    sqlCommand.Parameters.AddWithValue("@ExecutionId", executionId);
                    string status = sqlCommand.ExecuteScalar().ToString();
                    return status switch
                    {
                        "COMPLETED" or "WARNING" => new ExecutionResult.Success(),
                        "FAILED" => new ExecutionResult.Failure("Sub-execution failed"),
                        "STOPPED" => new ExecutionResult.Failure("Sub-execution was stopped"),
                        "SUSPENDED" => new ExecutionResult.Failure("Sub-execution was suspended"),
                        "NOT STARTED" => new ExecutionResult.Failure("Sub-execution failed to start"),
                        "RUNNING" => new ExecutionResult.Failure("Sub-execution was finished but its status was reported as RUNNING after finishing"),
                        _ => new ExecutionResult.Failure("Unhandled sub-execution status"),
                    };
                }
                catch (Exception ex)
                {
                    Log.Error(ex, "{ExecutionId} {StepId} Error getting sub-execution status for execution id {executionId}", executionConfig.ExecutionId, jobStep.StepId, executionId);
                    return new ExecutionResult.Failure("Error getting sub-execution status");
                }
            }

            return new ExecutionResult.Success();
        }
    }
}
