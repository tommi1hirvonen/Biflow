using Serilog;
using System;
using System.Data.SqlClient;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;

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

        public async Task<ExecutionResult> RunAsync()
        {

            Process executorProcess;
            string executionId;

            using (var sqlConnection = new SqlConnection(executionConfig.ConnectionString))
            {
                await sqlConnection.OpenAsync();

                var initCommand = new SqlCommand(
                        "EXEC etlmanager.ExecutionInitialize @JobId = @JobId_"
                        , sqlConnection);
                initCommand.Parameters.AddWithValue("@JobId_", jobStep.JobToExecuteId);

                try
                {
                    executionId = (await initCommand.ExecuteScalarAsync()).ToString();
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

                var processIdCmd = new SqlCommand("UPDATE etlmanager.Execution SET ExecutorProcessId = @ProcessId WHERE ExecutionId = @ExecutionId", sqlConnection);
                processIdCmd.Parameters.AddWithValue("@ProcessId", executorProcess.Id);
                processIdCmd.Parameters.AddWithValue("@ExecutionId", executionId);

                try
                {
                    await processIdCmd.ExecuteNonQueryAsync();
                }
                catch (Exception ex)
                {
                    Log.Error(ex, "{ExecutionId} {StepId} Error updating executor process id for execution {executionId}", executionConfig.ExecutionId, jobStep.StepId, executionId);
                }

            }

            if (jobStep.JobExecuteSynchronized)
            {
                await executorProcess.WaitForExitAsync();
                try
                {
                    using SqlConnection sqlConnection = new SqlConnection(executionConfig.ConnectionString);
                    await sqlConnection.OpenAsync();
                    var sqlCommand = new SqlCommand("SELECT TOP 1 ExecutionStatus FROM etlmanager.vExecutionJob WHERE ExecutionId = @ExecutionId", sqlConnection);
                    sqlCommand.Parameters.AddWithValue("@ExecutionId", executionId);
                    string status = (await sqlCommand.ExecuteScalarAsync()).ToString();
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
