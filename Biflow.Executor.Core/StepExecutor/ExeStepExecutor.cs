using Biflow.Executor.Core.Common;
using Microsoft.Extensions.Logging;
using System.Diagnostics;

namespace Biflow.Executor.Core.StepExecutor;

internal class ExeStepExecutor(
    ILogger<ExeStepExecutor> logger,
    IDbContextFactory<ExecutorDbContext> dbContextFactory)
    : StepExecutor<ExeStepExecution, ExeStepExecutionAttempt>(logger, dbContextFactory)
{
    private readonly ILogger<ExeStepExecutor> _logger = logger;
    private readonly IDbContextFactory<ExecutorDbContext> _dbContextFactory = dbContextFactory;

    protected override async Task<Result> ExecuteAsync(
        ExeStepExecution step,
        ExeStepExecutionAttempt attempt,
        ExtendedCancellationTokenSource cancellationTokenSource)
    {
        var cancellationToken = cancellationTokenSource.Token;
        cancellationToken.ThrowIfCancellationRequested();

        var startInfo = new ProcessStartInfo()
        {
            FileName = step.ExeFileName,
            UseShellExecute = false,
            CreateNoWindow = true,
            WindowStyle = ProcessWindowStyle.Hidden,
            RedirectStandardError = true,
            RedirectStandardOutput = true
        };

        if (!string.IsNullOrWhiteSpace(step.ExeArguments))
        {
            var parameters = step.StepExecutionParameters.ToStringDictionary();
            startInfo.Arguments = step.ExeArguments.Replace(parameters);
        }

        if (!string.IsNullOrWhiteSpace(step.ExeWorkingDirectory))
        {
            startInfo.WorkingDirectory = step.ExeWorkingDirectory;
        }

        var cred = step.GetRunAsCredential();
        if (OperatingSystem.IsWindows() && cred is not null)
        {
            startInfo.Domain = cred.Domain.NullIfEmpty();
            startInfo.UserName = cred.Username;
            startInfo.PasswordInClearText = cred.Password.NullIfEmpty();
            startInfo.LoadUserProfile = true;
        }

        using var process = new Process() { StartInfo = startInfo };
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
            _logger.LogError(ex, "{ExecutionId} {Step} Error starting process for file name {FileName}", step.ExecutionId, step, step.ExeFileName);
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
            _logger.LogError(ex, "{ExecutionId} {Step} Error logging child process id", step.ExecutionId, step);
            attempt.AddWarning(ex, "Error logging child process id");
        }

        using var timeoutCts = step.TimeoutMinutes > 0
            ? new CancellationTokenSource(TimeSpan.FromMinutes(step.TimeoutMinutes))
            : new CancellationTokenSource();
        
        try
        {
            using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, timeoutCts.Token);

            await process.WaitForExitAsync(linkedCts.Token);

            // If SuccessExitCode was defined, check the actual ExitCode. If SuccessExitCode is not defined, then report success in any case (not applicable).
            if (step.ExeSuccessExitCode is null || process.ExitCode == step.ExeSuccessExitCode)
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
                _logger.LogError(ex, "{ExecutionId} {Step} Error killing process after timeout", step.ExecutionId, step);
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
            _logger.LogError(ex, "{ExecutionId} {Step} Error while executing {FileName}", step.ExecutionId, step, step.ExeFileName);
            attempt.AddError(ex, "Error while executing exe");
            return Result.Failure;
        }
    }
}
