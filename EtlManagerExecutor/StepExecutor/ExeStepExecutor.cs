using EtlManagerDataAccess;
using EtlManagerDataAccess.Models;
using Microsoft.EntityFrameworkCore;
using Serilog;
using System;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace EtlManagerExecutor
{
    class ExeStepExecutor : StepExecutorBase
    {
        private ExeStepExecution Step { get; init; }

        private StringBuilder ErrorMessageBuilder { get; } = new StringBuilder();
        private StringBuilder OutputMessageBuilder { get; } = new StringBuilder();

        public ExeStepExecutor(IDbContextFactory<EtlManagerContext> dbContextFactory, ExeStepExecution step)
            : base(dbContextFactory, step)
        {
            Step = step;
        }

        protected override async Task<Result> ExecuteAsync(ExtendedCancellationTokenSource cancellationTokenSource)
        {
            var cancellationToken = cancellationTokenSource.Token;
            cancellationToken.ThrowIfCancellationRequested();

            var startInfo = new ProcessStartInfo()
            {
                FileName = Step.ExeFileName,
                UseShellExecute = false,
                CreateNoWindow = true,
                WindowStyle = ProcessWindowStyle.Hidden,
                RedirectStandardError = true,
                RedirectStandardOutput = true
            };

            if (!string.IsNullOrWhiteSpace(Step.ExeArguments))
                startInfo.Arguments = Step.ExeArguments;

            if (!string.IsNullOrWhiteSpace(Step.ExeWorkingDirectory))
                startInfo.WorkingDirectory = Step.ExeWorkingDirectory;

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
                Log.Error(ex, "{ExecutionId} {Step} Error starting process for file name {FileName}", Step.ExecutionId, Step, Step.ExeFileName);
                return Result.Failure("Error starting process: " + ex.Message);
            }

            try
            {
                var processTask = process.WaitForExitAsync(cancellationToken);

                // Convert timeout minutes to milliseconds if provided, otherwise -1 to wait indefinitely.
                var timeoutMs = Step.TimeoutMinutes > 0 ? Step.TimeoutMinutes * 60 * 1000 : -1;
                var timeoutTask = Task.Delay(timeoutMs, cancellationToken);

                // Wait for either the process to finish or for timeout.
                await Task.WhenAny(processTask, timeoutTask);

                // If the process has not finished, throw OperationCanceledException to begin cleanup.
                if (!process.HasExited)
                {
                    throw new OperationCanceledException();
                }
                // If SuccessExitCode was defined, check the actual ExitCode. If SuccessExitCode is not defined, then report success in any case (not applicable).
                else if (Step.ExeSuccessExitCode is null || process.ExitCode == Step.ExeSuccessExitCode)
                {
                    return Result.Success(OutputMessageBuilder.ToString());
                }
                else
                {
                    var errorMessage = $"{ErrorMessageBuilder}\n\nProcess finished with exit code {process.ExitCode}";
                    return Result.Failure(errorMessage, OutputMessageBuilder.ToString());
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
                    Log.Error(ex, "{ExecutionId} {Step} Error killing process after timeout", Step.ExecutionId, Step);
                }
                
                throw;
            }
            catch (Exception ex)
            {
                Log.Error(ex, "{ExecutionId} {Step} Error while executing {FileName}", Step.ExecutionId, Step, Step.ExeFileName);
                return Result.Failure("Error while executing exe: " + ex.Message);
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
