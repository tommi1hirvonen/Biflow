using Biflow.Core.Entities;
using Biflow.Executor.Core.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Diagnostics;

namespace Biflow.Executor.Core.StepExecutor;

internal class ExeStepExecutor(
    ILogger<ExeStepExecutor> logger,
    IDbContextFactory<ExecutorDbContext> dbContextFactory,
    ExeStepExecution step) : IStepExecutor<ExeStepExecutionAttempt>
{
    private readonly ILogger<ExeStepExecutor> _logger = logger;
    private readonly IDbContextFactory<ExecutorDbContext> _dbContextFactory = dbContextFactory;
    private readonly ExeStepExecution _step = step;

    public ExeStepExecutionAttempt Clone(ExeStepExecutionAttempt other, int retryAttemptIndex) =>
        new(other, retryAttemptIndex);

    public async Task<Result> ExecuteAsync(ExeStepExecutionAttempt attempt, ExtendedCancellationTokenSource cancellationTokenSource)
    {
        var cancellationToken = cancellationTokenSource.Token;
        cancellationToken.ThrowIfCancellationRequested();

        var startInfo = new ProcessStartInfo()
        {
            FileName = _step.ExeFileName,
            UseShellExecute = false,
            CreateNoWindow = true,
            WindowStyle = ProcessWindowStyle.Hidden,
            RedirectStandardError = true,
            RedirectStandardOutput = true
        };

        if (!string.IsNullOrWhiteSpace(_step.ExeArguments))
        {
            var parameters = _step.StepExecutionParameters.ToStringDictionary();
            startInfo.Arguments = _step.ExeArguments.Replace(parameters);
        }

        if (!string.IsNullOrWhiteSpace(_step.ExeWorkingDirectory))
        {
            startInfo.WorkingDirectory = _step.ExeWorkingDirectory;
        }

        var process = new Process() { StartInfo = startInfo };
        process.OutputDataReceived += (s, e) => attempt.AddOutput(e.Data);
        process.ErrorDataReceived += (s, e) => attempt.AddError(e.Data);

        try
        {
            process.Start();
            process.BeginErrorReadLine();
            process.BeginOutputReadLine();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "{ExecutionId} {Step} Error starting process for file name {FileName}", _step.ExecutionId, _step, _step.ExeFileName);
            attempt.AddError(ex, "Error starting process");
            return Result.Failure;
        }

        try
        {
            using var context = _dbContextFactory.CreateDbContext();
            attempt.ExeProcessId = process.Id;
            context.Attach(attempt);
            context.Entry(attempt).Property(p => p.ExeProcessId).IsModified = true;
            await context.SaveChangesAsync(CancellationToken.None);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "{ExecutionId} {Step} Error logging child process id", _step.ExecutionId, _step);
            attempt.AddWarning(ex, "Error logging child process id");
        }

        using var timeoutCts = _step.TimeoutMinutes > 0
            ? new CancellationTokenSource(TimeSpan.FromMinutes(_step.TimeoutMinutes))
            : new CancellationTokenSource();
        
        try
        {
            using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, timeoutCts.Token);

            await process.WaitForExitAsync(linkedCts.Token);

            // If SuccessExitCode was defined, check the actual ExitCode. If SuccessExitCode is not defined, then report success in any case (not applicable).
            if (_step.ExeSuccessExitCode is null || process.ExitCode == _step.ExeSuccessExitCode)
            {
                return Result.Success;
            }
            else
            {
                attempt.AddError($"Process finished with exit code {process.ExitCode}");
                return Result.Failure;
            }
        }
        catch (OperationCanceledException cancelEx)
        {
            // In case of cancellation or timeout 
            try
            {
                process.Kill();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "{ExecutionId} {Step} Error killing process after timeout", _step.ExecutionId, _step);
                attempt.AddWarning(ex, "Error killing process after timeout");
            }

            if (timeoutCts.IsCancellationRequested)
            {
                attempt.AddError(cancelEx, "Executing exe timed out");
                return Result.Failure;
            }
            attempt.AddWarning(cancelEx);
            return Result.Cancel;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "{ExecutionId} {Step} Error while executing {FileName}", _step.ExecutionId, _step, _step.ExeFileName);
            attempt.AddError(ex, "Error while executing exe");
            return Result.Failure;
        }
    }
}
