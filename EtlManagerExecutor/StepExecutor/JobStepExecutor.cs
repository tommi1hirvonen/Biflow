using Dapper;
using EtlManagerDataAccess.Models;
using Microsoft.Data.SqlClient;
using Serilog;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Pipes;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace EtlManagerExecutor
{
    class JobStepExecutor : IStepExecutor
    {
        private readonly IExecutionConfiguration _executionConfiguration;

        private JobStepExecution Step { get; init; }

        public int RetryAttemptCounter { get; set; } = 0;

        public JobStepExecutor(IExecutionConfiguration executionConfiguration, JobStepExecution step)
        {
            _executionConfiguration = executionConfiguration;
            Step = step;
        }

        public async Task<ExecutionResult> ExecuteAsync(ExtendedCancellationTokenSource cancellationTokenSource)
        {
            var cancellationToken = cancellationTokenSource.Token;
            cancellationToken.ThrowIfCancellationRequested();

            Process executorProcess;
            Guid jobExecutionId;

            using (var sqlConnection = new SqlConnection(_executionConfiguration.ConnectionString))
            {
                await sqlConnection.OpenAsync(CancellationToken.None);

                try
                {
                    jobExecutionId = await sqlConnection.ExecuteScalarAsync<Guid>(
                        "EXEC etlmanager.ExecutionInitialize @JobId = @JobId_", new { JobId_ = Step.JobToExecuteId });
                }
                catch (Exception ex)
                {
                    Log.Error(ex, "{ExecutionId} {Step} Error initializing execution for job {jobId}", Step.ExecutionId, Step, Step.JobToExecuteId);
                    return new ExecutionResult.Failure("Error initializing job execution: " + ex.Message);
                }

                var executorFilePath = Process.GetCurrentProcess().MainModule?.FileName
                    ?? throw new ArgumentNullException("FileName", "Executor file path cannot be null");
                var executionInfo = new ProcessStartInfo()
                {
                    FileName = executorFilePath,
                    ArgumentList = {
                        "execute",
                        "--id",
                        jobExecutionId.ToString(),
                        _executionConfiguration.Notify ? "--notify" : ""
                    },
                    UseShellExecute = false,
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
                    Log.Error(ex, "{ExecutionId} {Step} Error starting executor process for execution {executionId}", Step.ExecutionId, Step, jobExecutionId);
                    return new ExecutionResult.Failure("Error starting executor process: " + ex.Message);
                }

            }

            if (Step.JobExecuteSynchronized)
            {
                try
                {
                    await executorProcess.WaitForExitAsync(cancellationToken);
                }
                catch (OperationCanceledException)
                {
                    await CancelAsync(jobExecutionId, cancellationTokenSource.Username);
                    throw;
                }

                try
                {
                    using var sqlConnection = new SqlConnection(_executionConfiguration.ConnectionString);
                    var status = await sqlConnection.ExecuteScalarAsync<string>(
                        "SELECT TOP 1 ExecutionStatus FROM etlmanager.Execution WHERE ExecutionId = @ExecutionId",
                        new { ExecutionId = jobExecutionId });
                    return status switch
                    {
                        "SUCCEEDED" or "WARNING" => new ExecutionResult.Success(),
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
                    Log.Error(ex, "{ExecutionId} {Step} Error getting sub-execution status for execution id {executionId}", Step.ExecutionId, Step, jobExecutionId);
                    return new ExecutionResult.Failure("Error getting sub-execution status");
                }
            }

            return new ExecutionResult.Success();
        }

        private static async Task CancelAsync(Guid executionId, string username)
        {
            // Connect to the pipe server set up by the executor process.
            using var pipeClient = new NamedPipeClientStream(".", executionId.ToString().ToLower(), PipeDirection.Out); // "." => the pipe server is on the same computer
            await pipeClient.ConnectAsync(10000); // wait for 10 seconds
            using var streamWriter = new StreamWriter(pipeClient);
            // Send cancel command.
            var username_ = string.IsNullOrWhiteSpace(username) ? "unknown" : username;
            var cancelCommand = new { StepId = (string?)null, Username = username_ };
            var json = JsonSerializer.Serialize(cancelCommand);
            streamWriter.WriteLine(json);
        }
    }
}
