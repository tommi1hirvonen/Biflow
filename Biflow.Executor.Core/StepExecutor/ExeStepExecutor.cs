using Biflow.DataAccess;
using Biflow.DataAccess.Models;
using Biflow.Executor.Core.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Diagnostics;
using System.Text;

namespace Biflow.Executor.Core.StepExecutor;

internal class ExeStepExecutor : StepExecutorBase
{
    private readonly ILogger<ExeStepExecutor> _logger;
    private readonly IDbContextFactory<BiflowContext> _dbContextFactory;

    private ExeStepExecution Step { get; }

    private StringBuilder ErrorMessageBuilder { get; } = new StringBuilder();
    private StringBuilder OutputMessageBuilder { get; } = new StringBuilder();

    public ExeStepExecutor(ILogger<ExeStepExecutor> logger, IDbContextFactory<BiflowContext> dbContextFactory, ExeStepExecution step)
        : base(logger, dbContextFactory, step)
    {
        _logger = logger;
        _dbContextFactory = dbContextFactory;
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
        {
            var parameters = Step.StepExecutionParameters.ToStringDictionary();
            startInfo.Arguments = Step.ExeArguments.Replace(parameters);
        }

        if (!string.IsNullOrWhiteSpace(Step.ExeWorkingDirectory))
        {
            startInfo.WorkingDirectory = Step.ExeWorkingDirectory;
        }

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
            _logger.LogError(ex, "{ExecutionId} {Step} Error starting process for file name {FileName}", Step.ExecutionId, Step, Step.ExeFileName);
            return Result.Failure($"Error starting process:\n{ex.Message}");
        }

        try
        {
            var executionAttempt = Step.StepExecutionAttempts.MaxBy(e => e.RetryAttemptIndex);
            if (executionAttempt is ExeStepExecutionAttempt exe)
            {
                using var context = _dbContextFactory.CreateDbContext();
                exe.ExeProcessId = process.Id;
                context.Attach(exe);
                context.Entry(exe).Property(p => p.ExeProcessId).IsModified = true;
                await context.SaveChangesAsync(CancellationToken.None);
            }
            else
            {
                throw new InvalidOperationException("Could not find ExeStepExecutionAttempt from StepExecution");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "{ExecutionId} {Step} Error logging child process id", Step.ExecutionId, Step);
        }

        try
        {
            var processTask = process.WaitForExitAsync(cancellationToken);

            // Convert timeout minutes to milliseconds if provided, otherwise -1 to wait indefinitely.
            var timeoutTask = Step.TimeoutMinutes > 0
                ? Task.Delay(TimeSpan.FromMinutes(Step.TimeoutMinutes))
                : Task.Delay(-1, cancellationToken);

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
                _logger.LogError(ex, "{ExecutionId} {Step} Error killing process after timeout", Step.ExecutionId, Step);
            }

            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "{ExecutionId} {Step} Error while executing {FileName}", Step.ExecutionId, Step, Step.ExeFileName);
            return Result.Failure($"Error while executing exe:\n{ex.Message}");
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
