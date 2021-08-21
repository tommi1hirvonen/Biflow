using Dapper;
using EtlManagerDataAccess;
using EtlManagerDataAccess.Models;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
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
    class JobStepExecutor : StepExecutorBase
    {
        private readonly IExecutionConfiguration _executionConfiguration;

        private JobStepExecution Step { get; init; }

        public JobStepExecutor(
            IDbContextFactory<EtlManagerContext> dbContextFactory,
            IExecutionConfiguration executionConfiguration,
            JobStepExecution step)
            : base(dbContextFactory, step)
        {
            _executionConfiguration = executionConfiguration;
            Step = step;
        }

        protected override async Task<Result> ExecuteAsync(ExtendedCancellationTokenSource cancellationTokenSource)
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
                    return Result.Failure("Error initializing job execution: " + ex.Message);
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
                    return Result.Failure("Error starting executor process: " + ex.Message);
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
                        "SUCCEEDED" or "WARNING" => Result.Success(),
                        "FAILED" => Result.Failure("Sub-execution failed"),
                        "STOPPED" => Result.Failure("Sub-execution was stopped"),
                        "SUSPENDED" => Result.Failure("Sub-execution was suspended"),
                        "NOT STARTED" => Result.Failure("Sub-execution failed to start"),
                        "RUNNING" => Result.Failure("Sub-execution was finished but its status was reported as RUNNING after finishing"),
                        _ => Result.Failure("Unhandled sub-execution status"),
                    };
                }
                catch (Exception ex)
                {
                    Log.Error(ex, "{ExecutionId} {Step} Error getting sub-execution status for execution id {executionId}", Step.ExecutionId, Step, jobExecutionId);
                    return Result.Failure("Error getting sub-execution status");
                }
            }

            return Result.Success();
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
