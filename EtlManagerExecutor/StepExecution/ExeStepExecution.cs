using Dapper;
using Serilog;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace EtlManagerExecutor
{
    class ExeStepExecutionBuilder : IStepExecutionBuilder
    {
        public async Task<StepExecutionBase> CreateAsync(ExecutionConfiguration config, Step step, SqlConnection sqlConnection)
        {
            (var fileName, var arguments, var workingDirectory, var successExitCode, var timeoutMinutes) =
                await sqlConnection.QueryFirstAsync<(string, string, string, int?, int)>(
                    @"SELECT TOP 1
                        ExeFileName,
                        ExeArguments,
                        ExeWorkingDirectory,
                        ExeSuccessExitCode,
                        TimeoutMinutes
                    FROM etlmanager.Execution with (nolock)
                    WHERE ExecutionId = @ExecutionId AND StepId = @StepId",
                    new { config.ExecutionId, step.StepId });
            return new ExeStepExecution(config, step, fileName, arguments, workingDirectory, successExitCode, timeoutMinutes);
        }
    }

    class ExeStepExecution : StepExecutionBase
    {
        private string FileName { get; init; }
        private string Arguments { get; init; }
        private string WorkingDirectory { get; init; }
        private int? SuccessExitCode { get; init; }
        private int TimeoutMinutes { get; init; }

        private StringBuilder ErrorMessageBuilder { get; } = new StringBuilder();
        private StringBuilder OutputMessageBuilder { get; } = new StringBuilder();

        public ExeStepExecution(ExecutionConfiguration configuration, Step step, string fileName, string arguments, string workingDirectory, int? succcessExitCode, int timeoutMinutes)
            : base(configuration, step)
        {
            FileName = fileName;
            Arguments = arguments;
            WorkingDirectory = workingDirectory;
            TimeoutMinutes = timeoutMinutes;
            SuccessExitCode = succcessExitCode;
        }

        public override async Task<ExecutionResult> ExecuteAsync(CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var startInfo = new ProcessStartInfo()
            {
                FileName = FileName,
                UseShellExecute = false,
                CreateNoWindow = true,
                WindowStyle = ProcessWindowStyle.Hidden,
                RedirectStandardError = true,
                RedirectStandardOutput = true
            };

            if (!string.IsNullOrWhiteSpace(Arguments))
                startInfo.Arguments = Arguments;

            if (!string.IsNullOrWhiteSpace(WorkingDirectory))
                startInfo.WorkingDirectory = WorkingDirectory;

            var process = new Process() { StartInfo = startInfo };
            process.OutputDataReceived += new DataReceivedEventHandler(OutputDataReceived);
            process.ErrorDataReceived += new DataReceivedEventHandler(ErrorDataReceived);

            try
            {
                process.Start();
                process.BeginErrorReadLine();
                process.BeginOutputReadLine();
            }
            catch (Exception ex)
            {
                Log.Error(ex, "{ExecutionId} {Step} Error starting process for file name {FileName}", Configuration.ExecutionId, Step, FileName);
                return new ExecutionResult.Failure("Error starting process: " + ex.Message);
            }

            try
            {
                var processTask = process.WaitForExitAsync(cancellationToken);

                // Convert timeout minutes to milliseconds if provided, otherwise -1 to wait indefinitely.
                var timeoutMs = TimeoutMinutes > 0 ? TimeoutMinutes * 60 * 1000 : -1;
                var timeoutTask = Task.Delay(timeoutMs, cancellationToken);

                // Wait for either the process to finish or for timeout.
                await Task.WhenAny(processTask, timeoutTask);

                // If the process has not finished, throw OperationCanceledException to begin cleanup.
                if (!process.HasExited)
                {
                    throw new OperationCanceledException();
                }
                // If SuccessExitCode was defined, check the actual ExitCode. If SuccessExitCode is not defined, then report success in any case (not applicable).
                else if (SuccessExitCode is null || process.ExitCode == SuccessExitCode)
                {
                    return new ExecutionResult.Success(OutputMessageBuilder.ToString());
                }
                else
                {
                    var errorMessage = $"{ErrorMessageBuilder}\n\nProcess finished with exit code {process.ExitCode}";
                    return new ExecutionResult.Failure(errorMessage, OutputMessageBuilder.ToString());
                }
            }
            catch (OperationCanceledException)
            {
                // In case of cancellation or timeout 
                try
                {
                    process.Kill();
                }
                catch (Exception ex)
                {
                    Log.Error(ex, "{ExecutionId} {Step} Error killing process after timeout", Configuration.ExecutionId, Step);
                }
                
                throw;
            }
            catch (Exception ex)
            {
                Log.Error(ex, "{ExecutionId} {Step} Error while executing {FileName}", Configuration.ExecutionId, Step, FileName);
                return new ExecutionResult.Failure("Error while executing exe: " + ex.Message);
            }
        }

        private void ErrorDataReceived(object sender, DataReceivedEventArgs e)
        {
            ErrorMessageBuilder.AppendLine(e.Data);
        }

        private void OutputDataReceived(object sender, DataReceivedEventArgs e)
        {
            OutputMessageBuilder.AppendLine(e.Data);
        }

    }
}
