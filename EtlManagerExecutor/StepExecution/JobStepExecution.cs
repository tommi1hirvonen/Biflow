using Serilog;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Diagnostics;
using System.IO;
using System.IO.Pipes;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace EtlManagerExecutor
{
    class JobStepExecution : IExecutable
    {
        private ExecutionConfiguration Configuration { get; init; }
        public string StepId { get; init; }
        private string JobToExecuteId { get; init; }
        private bool JobExecuteSynchronized { get; init; }
        public int RetryAttemptCounter { get; set; }

        public JobStepExecution(ExecutionConfiguration configuration, string stepId, string jobToExecuteId, bool jobExecuteSynchronized)
        {
            Configuration = configuration;
            StepId = stepId;
            JobToExecuteId = jobToExecuteId;
            JobExecuteSynchronized = jobExecuteSynchronized;
        }

        public async Task<ExecutionResult> ExecuteAsync(CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            Process executorProcess;
            string jobExecutionId;

            using (var sqlConnection = new SqlConnection(Configuration.ConnectionString))
            {
                await sqlConnection.OpenAsync(CancellationToken.None);

                var initCommand = new SqlCommand(
                        "EXEC etlmanager.ExecutionInitialize @JobId = @JobId_"
                        , sqlConnection);
                initCommand.Parameters.AddWithValue("@JobId_", JobToExecuteId);

                try
                {
                    jobExecutionId = (await initCommand.ExecuteScalarAsync(CancellationToken.None)).ToString();
                }
                catch (Exception ex)
                {
                    Log.Error(ex, "{ExecutionId} {StepId} Error initializing execution for job {jobId}", Configuration.ExecutionId, StepId, JobToExecuteId);
                    return new ExecutionResult.Failure("Error initializing job execution: " + ex.Message);
                }

                string executorFilePath = Process.GetCurrentProcess().MainModule.FileName;
                ProcessStartInfo executionInfo = new ProcessStartInfo()
                {
                    FileName = executorFilePath,
                    ArgumentList = {
                        "execute",
                        "--id",
                        jobExecutionId.ToString(),
                        Configuration.Notify ? "--notify" : ""
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
                    Log.Error(ex, "{ExecutionId} {StepId} Error starting executor process for execution {executionId}", Configuration.ExecutionId, StepId, jobExecutionId);
                    return new ExecutionResult.Failure("Error starting executor process: " + ex.Message);
                }

                var processIdCmd = new SqlCommand("UPDATE etlmanager.Execution SET ExecutorProcessId = @ProcessId WHERE ExecutionId = @ExecutionId", sqlConnection);
                processIdCmd.Parameters.AddWithValue("@ProcessId", executorProcess.Id);
                processIdCmd.Parameters.AddWithValue("@ExecutionId", jobExecutionId);

                try
                {
                    await processIdCmd.ExecuteNonQueryAsync(CancellationToken.None);
                }
                catch (Exception ex)
                {
                    Log.Error(ex, "{ExecutionId} {StepId} Error updating executor process id for execution {executionId}", Configuration.ExecutionId, StepId, jobExecutionId);
                }

            }

            if (JobExecuteSynchronized)
            {
                try
                {
                    await executorProcess.WaitForExitAsync(cancellationToken);
                }
                catch (OperationCanceledException)
                {
                    await CancelAsync(jobExecutionId, Configuration.Username);
                    throw;
                }

                try
                {
                    using SqlConnection sqlConnection = new SqlConnection(Configuration.ConnectionString);
                    await sqlConnection.OpenAsync(CancellationToken.None);
                    var sqlCommand = new SqlCommand("SELECT TOP 1 ExecutionStatus FROM etlmanager.vExecutionJob WHERE ExecutionId = @ExecutionId", sqlConnection);
                    sqlCommand.Parameters.AddWithValue("@ExecutionId", jobExecutionId);
                    string status = (await sqlCommand.ExecuteScalarAsync(CancellationToken.None)).ToString();
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
                    Log.Error(ex, "{ExecutionId} {StepId} Error getting sub-execution status for execution id {executionId}", Configuration.ExecutionId, StepId, jobExecutionId);
                    return new ExecutionResult.Failure("Error getting sub-execution status");
                }
            }

            return new ExecutionResult.Success();
        }

        private static async Task CancelAsync(string executionId, string username)
        {
            // Connect to the pipe server set up by the executor process.
            using var pipeClient = new NamedPipeClientStream(".", executionId.ToString().ToLower(), PipeDirection.Out); // "." => the pipe server is on the same computer
            await pipeClient.ConnectAsync(10000); // wait for 10 seconds
            using var streamWriter = new StreamWriter(pipeClient);
            // Send cancel command.
            var username_ = string.IsNullOrWhiteSpace(username) ? "unknown" : username;
            streamWriter.WriteLine(username_);
        }
    }
}
