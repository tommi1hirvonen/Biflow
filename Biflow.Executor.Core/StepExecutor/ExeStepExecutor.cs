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

    private StringBuilder Warning { get; } = new StringBuilder();
    private StringBuilder Error { get; } = new StringBuilder();
    private StringBuilder Output { get; } = new StringBuilder();

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
            Warning.AppendLine($"Error logging child process id:\n{ex.Message}");
        }

        using var timeoutCts = Step.TimeoutMinutes > 0
            ? new CancellationTokenSource(TimeSpan.FromMinutes(Step.TimeoutMinutes))
            : new CancellationTokenSource();
        
        try
        {
            using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, timeoutCts.Token);

            await process.WaitForExitAsync(linkedCts.Token);

            // If SuccessExitCode was defined, check the actual ExitCode. If SuccessExitCode is not defined, then report success in any case (not applicable).
            if (Step.ExeSuccessExitCode is null || process.ExitCode == Step.ExeSuccessExitCode)
            {
                return Result.Success(Output.ToString(), Warning.ToString());
            }
            else
            {
                var errorMessage = $"{Error}\n\nProcess finished with exit code {process.ExitCode}";
                return Result.Failure(errorMessage, Warning.ToString(), Output.ToString());
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
                Warning.AppendLine($"Error killing process after timeout:\n{ex.Message}");
            }

            if (timeoutCts.IsCancellationRequested)
            {
                return Result.Failure($"Executing exe timed out", Warning.ToString(), Output.ToString()); // Report failure => allow possible retries
            }

            throw; // Step was canceled => pass the exception => no retries
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "{ExecutionId} {Step} Error while executing {FileName}", Step.ExecutionId, Step, Step.ExeFileName);
            return Result.Failure($"Error while executing exe:\n{ex.Message}", Warning.ToString(), Output.ToString());
        }
    }

    private void ErrorDataReceived(object sender, DataReceivedEventArgs e)
    {
        Error.AppendLine(e.Data);
    }

    private void OutputDataReceived(object sender, DataReceivedEventArgs e)
    {
        Output.AppendLine(e.Data);
    }

}
