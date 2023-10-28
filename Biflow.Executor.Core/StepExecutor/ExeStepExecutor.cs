using Biflow.DataAccess.Models;
using Biflow.Executor.Core.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Diagnostics;
using System.Text;

namespace Biflow.Executor.Core.StepExecutor;

internal class ExeStepExecutor(
    ILogger<ExeStepExecutor> logger,
    IDbContextFactory<ExecutorDbContext> dbContextFactory,
    ExeStepExecution step) : StepExecutorBase(logger, dbContextFactory, step)
{
    private readonly ILogger<ExeStepExecutor> _logger = logger;
    private readonly IDbContextFactory<ExecutorDbContext> _dbContextFactory = dbContextFactory;

    private ExeStepExecution Step { get; } = step;

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
        process.OutputDataReceived += (object sender, DataReceivedEventArgs e) => AddOutput(e.Data);
        process.ErrorDataReceived += (object sender, DataReceivedEventArgs e) => AddError(e.Data);

        try
        {
            process.Start();
            process.BeginErrorReadLine();
            process.BeginOutputReadLine();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "{ExecutionId} {Step} Error starting process for file name {FileName}", Step.ExecutionId, Step, Step.ExeFileName);
            AddError(ex, "Error starting process");
            return Result.Failure;
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
            AddWarning(ex, "Error logging child process id");
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
                return Result.Success;
            }
            else
            {
                AddError($"Process finished with exit code {process.ExitCode}");
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
                _logger.LogError(ex, "{ExecutionId} {Step} Error killing process after timeout", Step.ExecutionId, Step);
                AddWarning(ex, "Error killing process after timeout");
            }

            if (timeoutCts.IsCancellationRequested)
            {
                AddError(cancelEx, "Executing exe timed out");
                return Result.Failure;
            }
            AddWarning(cancelEx);
            return Result.Cancel;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "{ExecutionId} {Step} Error while executing {FileName}", Step.ExecutionId, Step, Step.ExeFileName);
            AddError(ex, "Error while executing exe");
            return Result.Failure;
        }
    }
}
