using Microsoft.Extensions.Logging;
using System.Diagnostics;
using System.Text;
using System.Threading.Channels;
using Microsoft.Extensions.DependencyInjection;

namespace Biflow.Executor.Core.StepExecutor;

internal class LocalExeStepExecutor : IStepExecutor
{
    private const int MaxOutputLength = 500_000;
    
    private readonly InfoMessage _outputMessage = new("");
    private readonly ErrorMessage _errorMessage = new("", null);
    private readonly StringBuilder _outputBuilder = new();
    private readonly StringBuilder _errorBuilder = new();
    // Locks for updating and reading the output and error message builders.
    private readonly ReaderWriterLockSlim _outputLock = new();
    private readonly ReaderWriterLockSlim _errorLock = new();
    // Create bounded channels. The string builder with all outputs is passed, so capacity of 1 is enough.
    private readonly Channel<StringBuilder> _outputChannel = Channel.CreateBounded<StringBuilder>(1);
    private readonly Channel<StringBuilder> _errorChannel = Channel.CreateBounded<StringBuilder>(1);
    private readonly ILogger<LocalExeStepExecutor> _logger;
    private readonly IDbContextFactory<ExecutorDbContext> _dbContextFactory;
    private readonly ExeStepExecution _step;
    private readonly ExeStepExecutionAttempt _attempt;

    public LocalExeStepExecutor(
        IServiceProvider serviceProvider,
        ExeStepExecution step,
        ExeStepExecutionAttempt attempt)
    {
        _logger = serviceProvider.GetRequiredService<ILogger<LocalExeStepExecutor>>();
        _dbContextFactory = serviceProvider.GetRequiredService<IDbContextFactory<ExecutorDbContext>>();
        _step = step;
        _attempt = attempt;
        // Store messages in the attempt because the messages themselves will be updated periodically.
        _attempt.InfoMessages.Insert(0, _outputMessage);
        _attempt.ErrorMessages.Insert(0, _errorMessage);
    }

    public async Task<Result> ExecuteAsync(OrchestrationContext context, CancellationContext cancellationContext)
    {
        var cancellationToken = cancellationContext.CancellationToken;
        cancellationToken.ThrowIfCancellationRequested();
        
        var startInfo = new ProcessStartInfo
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

        var cred = _step.GetRunAsCredential();
        if (OperatingSystem.IsWindows() && cred is not null)
        {
            // A new process launched with the Process class runs in the same
            // window station and desktop as the launching process => grant permissions.
            WindowsExtensions.GrantAccessToWindowStationAndDesktop(cred.Domain, cred.Username);
            startInfo.Domain = cred.Domain?.NullIfEmpty();
            startInfo.UserName = cred.Username;
            startInfo.PasswordInClearText = cred.Password?.NullIfEmpty();
            startInfo.LoadUserProfile = true;
        }
        else if (cred is not null)
        {
            _attempt.AddWarning("Running executables with impersonation is only supported on Windows.");
        }

        using var process = new Process();
        process.StartInfo = startInfo;
        process.OutputDataReceived += OutputDataReceived;
        process.ErrorDataReceived += ErrorDataReceived;

        // Create periodic consumers to update info and error messages while the process is still running.
        // This way we can push updates to the database even if the process has not yet finished.
        // This can be useful in scenarios where the process is long-running and the user wants to see the progress.
        using var outputConsumer = CreateOutputChannelConsumer();
        var outputConsumerTask = outputConsumer.StartConsumingAsync(cancellationToken);

        using var errorConsumer = CreateErrorChannelConsumer();
        var errorConsumerTask = errorConsumer.StartConsumingAsync(cancellationToken);

        try
        {
            try
            {
                process.Start();
                process.BeginErrorReadLine();
                process.BeginOutputReadLine();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "{ExecutionId} {Step} Error starting process for file name {FileName}",
                    _step.ExecutionId, _step, _step.ExeFileName);
                _attempt.AddError(ex, "Error starting process");
                return Result.Failure;
            }

            try
            {
                await using var dbContext = await _dbContextFactory.CreateDbContextAsync(CancellationToken.None);
                _attempt.ExeProcessId = process.Id; // Might throw an exception
                await dbContext.Set<ExeStepExecutionAttempt>()
                    .Where(x => x.ExecutionId == _attempt.ExecutionId &&
                                x.StepId == _attempt.StepId &&
                                x.RetryAttemptIndex == _attempt.RetryAttemptIndex)
                    .ExecuteUpdateAsync(x => x
                        .SetProperty(p => p.ExeProcessId, _attempt.ExeProcessId), CancellationToken.None);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "{ExecutionId} {Step} Error logging child process id", _step.ExecutionId, _step);
                _attempt.AddWarning(ex, "Error logging child process id");
            }

            using var timeoutCts = _step.TimeoutMinutes > 0
                ? new CancellationTokenSource(TimeSpan.FromMinutes(_step.TimeoutMinutes))
                : new CancellationTokenSource();

            try
            {
                using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, timeoutCts.Token);

                await process.WaitForExitAsync(linkedCts.Token);

                // If SuccessExitCode was defined, check the actual ExitCode. If SuccessExitCode is not defined,
                // then report success in any case (not applicable).
                if (_step.ExeSuccessExitCode is null || process.ExitCode == _step.ExeSuccessExitCode)
                {
                    return Result.Success;
                }
                else
                {
                    _attempt.AddError($"Process finished with exit code {process.ExitCode}");
                    return Result.Failure;
                }
            }
            catch (OperationCanceledException cancelEx)
            {
                // In case of cancellation or timeout 
                try
                {
                    process.Kill(entireProcessTree: true);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "{ExecutionId} {Step} Error killing process after timeout", _step.ExecutionId,
                        _step);
                    _attempt.AddWarning(ex, "Error killing process after timeout");
                }

                if (timeoutCts.IsCancellationRequested)
                {
                    _attempt.AddError(cancelEx, "Executing exe timed out");
                    return Result.Failure;
                }

                _attempt.AddWarning(cancelEx);
                return Result.Cancel;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "{ExecutionId} {Step} Error while executing {FileName}", _step.ExecutionId, _step,
                    _step.ExeFileName);
                _attempt.AddError(ex, "Error while executing exe");
                return Result.Failure;
            }
        }
        finally
        {
            outputConsumer.Cancel();
            errorConsumer.Cancel();
            // Update the output and errors one last time in case
            // the periodic consumers did not have a chance to handle the latest updates from the channel.
            try
            {
                _ = UpdateOutput();
                _ = UpdateErrors();
            }
            catch (Exception ex) { _logger.LogError(ex, "Error updating final output and error messages"); }
            await outputConsumerTask;
            await errorConsumerTask;
        }
    }

    private void OutputDataReceived(object sender, DataReceivedEventArgs e)
    {
        if (_outputBuilder.Length >= MaxOutputLength || string.IsNullOrEmpty(e.Data)) return;
        try
        {
            _outputLock.EnterWriteLock();
            _outputBuilder.AppendLine(e.Data);
            _ = _outputChannel.Writer.TryWrite(_outputBuilder);
        }
        finally { _outputLock.ExitWriteLock(); }
    }
    
    private void ErrorDataReceived(object sender, DataReceivedEventArgs e)
    {
        if (_errorBuilder.Length >= MaxOutputLength || string.IsNullOrEmpty(e.Data)) return;
        try
        {
            _errorLock.EnterWriteLock();
            _errorBuilder.AppendLine(e.Data);
            _ = _errorChannel.Writer.TryWrite(_errorBuilder);
        }
        finally { _errorLock.ExitWriteLock(); }
    }

    private PeriodicChannelConsumer<StringBuilder> CreateOutputChannelConsumer() => new(
        logger: _logger,
        reader: _outputChannel.Reader,
        // Update every 10 seconds for the first 5 minutes (300 sec), then every 30 seconds.
        interval: iteration => iteration <= 30 ? TimeSpan.FromSeconds(10) : TimeSpan.FromSeconds(30),
        bufferPublished: (_, ct) => UpdateOutput() ? UpdateOutputToDbAsync(ct) : Task.CompletedTask);
    
    private PeriodicChannelConsumer<StringBuilder> CreateErrorChannelConsumer() => new(
        logger: _logger,
        reader: _errorChannel.Reader,
        interval: iteration => iteration <= 30 ? TimeSpan.FromSeconds(10) : TimeSpan.FromSeconds(30),
        bufferPublished: (_, ct) => UpdateErrors() ? UpdateErrorsToDbAsync(ct) : Task.CompletedTask);

    private bool UpdateOutput()
    {
        string text;
        try
        {
            _outputLock.EnterReadLock();
            text = _outputBuilder.ToString();
        }
        finally{ _outputLock.ExitReadLock();}
        return UpdateOutput(text);
    }
    
    private bool UpdateOutput(string text)
    {
        // The output is empty or there are no changes.
        if (string.IsNullOrEmpty(text) || text.Length == _outputMessage.Message.Length)
        {
            return false; // No changes
        }

        // The output max length has been reached, but the message was not yet marked as truncated.
        if (text.Length >= MaxOutputLength && _outputMessage.Message.Length >= MaxOutputLength && !_outputMessage.IsTruncated)
        {
            _outputMessage.IsTruncated = true;
            return true;
        }
        
        // Executable output and error messages can be significantly long. Handle super long messages here.
        _outputMessage.Message = text[..Math.Min(MaxOutputLength, text.Length)];
        _outputMessage.IsTruncated = text.Length > _outputMessage.Message.Length;
        return true; // Changes were made
    }

    private bool UpdateErrors()
    {
        string text;
        try
        {
            _errorLock.EnterReadLock();
            text = _errorBuilder.ToString();
        }
        finally { _errorLock.ExitReadLock(); }
        return UpdateErrors(text);
    }
    
    private bool UpdateErrors(string text)
    {
        // The error is empty or there are no changes.
        if (string.IsNullOrEmpty(text) || text.Length == _errorMessage.Message.Length)
        {
            return false; // No changes
        }

        // The error message max length has been reached, but the message was not yet marked as truncated.
        if (text.Length >= MaxOutputLength && _errorMessage.Message.Length >= MaxOutputLength && !_errorMessage.IsTruncated)
        {
            _errorMessage.IsTruncated = true;
            return true;
        }
        
        _errorMessage.Message = text[..Math.Min(MaxOutputLength, text.Length)];
        _errorMessage.IsTruncated = text.Length > _errorMessage.Message.Length;
        return true; // Changes were made
    }
    
    private async Task UpdateOutputToDbAsync(CancellationToken cancellationToken)
    {
        try
        {
            await using var dbContext = await _dbContextFactory.CreateDbContextAsync(cancellationToken);
            await dbContext.StepExecutionAttempts
                .Where(x => x.ExecutionId == _attempt.ExecutionId &&
                            x.StepId == _attempt.StepId &&
                            x.RetryAttemptIndex == _attempt.RetryAttemptIndex)
                .ExecuteUpdateAsync(
                    // The output InfoMessage should be included in the InfoMessages collection.
                    x => x.SetProperty(p => p.InfoMessages, _attempt.InfoMessages),
                    cancellationToken: cancellationToken);
        }
        catch (OperationCanceledException)
        {
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating output for step");
        }
    }
    
    private async Task UpdateErrorsToDbAsync(CancellationToken cancellationToken)
    {
        try
        {
            await using var dbContext = await _dbContextFactory.CreateDbContextAsync(cancellationToken);
            await dbContext.StepExecutionAttempts
                .Where(x => x.ExecutionId == _attempt.ExecutionId &&
                            x.StepId == _attempt.StepId &&
                            x.RetryAttemptIndex == _attempt.RetryAttemptIndex)
                .ExecuteUpdateAsync(
                    x => x.SetProperty(p => p.ErrorMessages, _attempt.ErrorMessages),
                    cancellationToken: cancellationToken);
        }
        catch (OperationCanceledException)
        {
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating error output for step");
        }
    }
}
