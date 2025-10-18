using Microsoft.Extensions.Logging;
using System.Diagnostics;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Threading.Channels;
using Biflow.ExecutorProxy.Core;

namespace Biflow.Executor.Core.StepExecutor;

[UsedImplicitly]
internal class ExeStepExecutor(
    IHttpClientFactory httpClientFactory,
    ILogger<ExeStepExecutor> logger,
    IDbContextFactory<ExecutorDbContext> dbContextFactory)
    : StepExecutor<ExeStepExecution, ExeStepExecutionAttempt>(logger, dbContextFactory)
{
    private readonly ILogger<ExeStepExecutor> _logger = logger;
    private readonly IHttpClientFactory _httpClientFactory = httpClientFactory;
    private readonly IDbContextFactory<ExecutorDbContext> _dbContextFactory = dbContextFactory;

    private const int MaxOutputLength = 500_000;

    protected override Task<Result> ExecuteAsync(
        OrchestrationContext context,
        ExeStepExecution step,
        ExeStepExecutionAttempt attempt,
        ExtendedCancellationTokenSource cancellationTokenSource)
    {
        var cancellationToken = cancellationTokenSource.Token;
        cancellationToken.ThrowIfCancellationRequested();

        if (step.GetProxy() is { } proxy)
        {
            return ExecuteRemoteAsync(step, attempt, proxy, cancellationToken);
        }

        return ExecuteLocalAsync(step, attempt, cancellationToken);
    }

    private async Task<Result> ExecuteLocalAsync(ExeStepExecution step, ExeStepExecutionAttempt attempt,
        CancellationToken cancellationToken)
    {
        var startInfo = new ProcessStartInfo
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
            attempt.AddWarning("Running executables with impersonation is only supported on Windows.");
        }

        // Create channels for output and error messages.
        // Store messages in the attempt because the messages themselves will be updated periodically.
        var (outputMessage, errorMessage) = (new InfoMessage(""), new ErrorMessage("", null));
        attempt.InfoMessages.Insert(0, outputMessage);
        attempt.ErrorMessages.Insert(0, errorMessage);
        var (outputBuilder, errorBuilder) = (new StringBuilder(), new StringBuilder());
        var (outputLock, errorLock) = (new ReaderWriterLockSlim(), new ReaderWriterLockSlim());
        // Create bounded channels. The string builder with all outputs is passed, so capacity of 1 is enough.
        var (outputChannel, errorChannel) = (Channel.CreateBounded<StringBuilder>(1), Channel.CreateBounded<StringBuilder>(1));

        using var process = new Process();
        process.StartInfo = startInfo;
        process.OutputDataReceived += (__, e) =>
        {
            if (outputBuilder.Length >= MaxOutputLength || string.IsNullOrEmpty(e.Data)) return;
            try
            {
                outputLock.EnterWriteLock();
                outputBuilder.AppendLine(e.Data);
                _ = outputChannel.Writer.TryWrite(outputBuilder);
            }
            finally { outputLock.ExitWriteLock(); }
        };
        process.ErrorDataReceived += (__, e) =>
        {
            if (errorBuilder.Length >= MaxOutputLength || string.IsNullOrEmpty(e.Data)) return;
            try
            {
                errorLock.EnterWriteLock();
                errorBuilder.AppendLine(e.Data);
                _ = errorChannel.Writer.TryWrite(errorBuilder);
            }
            finally { errorLock.ExitWriteLock(); }
        };

        // Create periodic consumers to update info and error messages while the process is still running.
        // This way we can push updates to the database even if the process has not yet finished.
        // This can be useful in scenarios where the process is long-running and the user wants to see the progress.
        using var outputConsumer = new PeriodicChannelConsumer<StringBuilder>(
            logger: _logger,
            reader: outputChannel.Reader,
            // Update every 10 seconds for the first 5 minutes (300 sec), then every 30 seconds.
            interval: iteration => iteration <= 30 ? TimeSpan.FromSeconds(10) : TimeSpan.FromSeconds(30),
            bufferPublished: (sb, ct) => UpdateOutputToDbAsync(attempt, outputMessage, sb, outputLock, ct));
        var outputConsumerTask = outputConsumer.StartConsumingAsync(cancellationToken);

        using var errorConsumer = new PeriodicChannelConsumer<StringBuilder>(
            logger: _logger,
            reader: errorChannel.Reader,
            interval: iteration => iteration <= 30 ? TimeSpan.FromSeconds(10) : TimeSpan.FromSeconds(30),
            bufferPublished: (sb, ct) => UpdateErrorsToDbAsync(attempt, errorMessage, sb, errorLock, ct));
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
                    step.ExecutionId, step, step.ExeFileName);
                attempt.AddError(ex, "Error starting process");
                return Result.Failure;
            }

            try
            {
                await using var dbContext = await _dbContextFactory.CreateDbContextAsync(CancellationToken.None);
                attempt.ExeProcessId = process.Id; // Might throw an exception
                await dbContext.Set<ExeStepExecutionAttempt>()
                    .Where(x => x.ExecutionId == attempt.ExecutionId &&
                                x.StepId == attempt.StepId &&
                                x.RetryAttemptIndex == attempt.RetryAttemptIndex)
                    .ExecuteUpdateAsync(x => x
                        .SetProperty(p => p.ExeProcessId, attempt.ExeProcessId), CancellationToken.None);
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

                // If SuccessExitCode was defined, check the actual ExitCode. If SuccessExitCode is not defined,
                // then report success in any case (not applicable).
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
                    process.Kill(entireProcessTree: true);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "{ExecutionId} {Step} Error killing process after timeout", step.ExecutionId,
                        step);
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
                _logger.LogError(ex, "{ExecutionId} {Step} Error while executing {FileName}", step.ExecutionId, step,
                    step.ExeFileName);
                attempt.AddError(ex, "Error while executing exe");
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
                _ = UpdateOutput(attempt, outputMessage, outputBuilder, outputLock);
                _ = UpdateErrors(attempt, errorMessage, errorBuilder, errorLock);
            }
            catch (Exception ex) { _logger.LogError(ex, "Error updating final output and error messages"); }
            await outputConsumerTask;
            await errorConsumerTask;
        }
    }

    private async Task<Result> ExecuteRemoteAsync(ExeStepExecution step, ExeStepExecutionAttempt attempt,
        Proxy proxy, CancellationToken cancellationToken)
    {
        var client = CreateProxyHttpClient(proxy);
        var request = CreateExeProxyRunRequest(step);

        using var response = await client.PostAsJsonAsync("/exe", request, cancellationToken);
        await using var contentStream = await response.Content.ReadAsStreamAsync(cancellationToken);
        var taskStartedResponse = JsonSerializer.Deserialize<TaskStartedResponse>(
            contentStream,
            JsonSerializerOptions.Web);

        if (taskStartedResponse is null)
        {
            _logger.LogError("{ExecutionId} {Step} Error starting remote execution, no task id was returned",
                step.ExecutionId, step);
            attempt.AddError("No task id was returned from the proxy when starting remote execution.");
            return Result.Failure;
        }
        
        using var timeoutCts = step.TimeoutMinutes > 0
            ? new CancellationTokenSource(TimeSpan.FromMinutes(step.TimeoutMinutes))
            : new CancellationTokenSource();
        
        using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, timeoutCts.Token);

        try
        {
            // TODO Periodically update the outputs from the status object to the app database, same as for local executions.
            ExeTaskStatusResponse? status;
            do
            {
                await Task.Delay(TimeSpan.FromSeconds(5), linkedCts.Token);
                
                status = await client.GetFromJsonAsync<ExeTaskStatusResponse>($"/exe/{taskStartedResponse.TaskId}",
                    linkedCts.Token);

                int? processId = status switch
                {
                    ExeTaskRunningResponse running => running.ProcessId,
                    ExeTaskCompletedResponse completed => completed.ProcessId,
                    _ => null
                };
                if (processId == attempt.ExeProcessId)
                {
                    continue;
                }
                attempt.ExeProcessId = processId;
                await UpdateProcessIdAsync(step, attempt);
            } while (status is ExeTaskRunningResponse);
            
            switch (status)
            {
                case null:
                    _logger.LogError("{ExecutionId} {Step} Error getting remote execution status, no status was returned",
                        step.ExecutionId, step);
                    attempt.AddError("No status was returned from the proxy when getting remote execution status.");
                    return Result.Failure;
                case ExeTaskCompletedResponse completed:
                    if (completed.OutputIsTruncated)
                        attempt.AddOutput("Output is truncated.");
                    attempt.AddOutput(completed.Output);
                    
                    if (completed.ErrorOutputIsTruncated)
                        attempt.AddError("Error output is truncated.");
                    if (!string.IsNullOrWhiteSpace(completed.ErrorOutput))
                        attempt.AddError(completed.ErrorOutput);
                    
                    if (step.ExeSuccessExitCode is { } successExitCode)
                    {
                        return completed.ExitCode == successExitCode ? Result.Success : Result.Failure;
                    }
                    
                    return Result.Success;
                case ExeTaskFailedResponse failed:
                    attempt.AddError("Remote execution failed with an internal error.");
                    attempt.AddError(failed.ErrorMessage);
                    return Result.Failure;
                default:
                    attempt.AddError($"Unrecognized status {status.GetType().Name} from the proxy when getting remote execution status.");
                    return Result.Failure; // Should never happen, but just in case.
            }
        }
        catch (OperationCanceledException cancelEx)
        {
            // In case of cancellation or timeout 
            try
            {
                await client.PostAsync($"/exe/{taskStartedResponse.TaskId}/cancel", null, CancellationToken.None);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "{ExecutionId} {Step} Error canceling remote execution after timeout",
                    step.ExecutionId,
                    step);
                attempt.AddWarning(ex, "Error canceling remote execution after timeout");
            }

            if (timeoutCts.IsCancellationRequested)
            {
                attempt.AddError(cancelEx, "Executing remote executable timed out");
                return Result.Failure;
            }

            attempt.AddWarning(cancelEx);
            return Result.Cancel;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "{ExecutionId} {Step} Error while executing remote executable {FileName}",
                step.ExecutionId,
                step,
                step.ExeFileName);
            attempt.AddError(ex, "Error while executing remote executable");
            return Result.Failure;
        }
    }

    private HttpClient CreateProxyHttpClient(Proxy proxy)
    {
        var client = _httpClientFactory.CreateClient();
        client.BaseAddress = new Uri(proxy.ProxyUrl);
        if (proxy.ApiKey is not null)
        {
            client.DefaultRequestHeaders.Add("x-api-key", proxy.ApiKey);
        }
        return client;
    }

    private static ExeProxyRunRequest CreateExeProxyRunRequest(ExeStepExecution step)
    {
        string? arguments;
        if (!string.IsNullOrWhiteSpace(step.ExeArguments))
        {
            var parameters = step.StepExecutionParameters.ToStringDictionary();
            arguments = step.ExeArguments.Replace(parameters);
        }
        else
        {
            arguments = null;
        }

        var workingDirectory = !string.IsNullOrWhiteSpace(step.ExeWorkingDirectory)
            ? step.ExeWorkingDirectory
            : null;
        var request = new ExeProxyRunRequest
        {
            ExePath = step.ExeFileName,
            Arguments = arguments,
            WorkingDirectory = workingDirectory
        };
        return request;
    }

    private async Task UpdateProcessIdAsync(ExeStepExecution step, ExeStepExecutionAttempt attempt)
    {
        try
        {
            await using var dbContext = await _dbContextFactory.CreateDbContextAsync();
            await dbContext.Set<ExeStepExecutionAttempt>()
                .Where(x => x.ExecutionId == attempt.ExecutionId &&
                            x.StepId == attempt.StepId &&
                            x.RetryAttemptIndex == attempt.RetryAttemptIndex)
                .ExecuteUpdateAsync(x => x.SetProperty(p => p.ExeProcessId, attempt.ExeProcessId));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "{ExecutionId} {Step} Error logging child process id", attempt.ExecutionId, step);
            attempt.AddWarning(ex, "Error logging child process id");
        }
    }

    private static bool UpdateOutput(ExeStepExecutionAttempt attempt, InfoMessage output,
        StringBuilder outputBuilder, ReaderWriterLockSlim outputLock)
    {
        string text;
        try
        {
            outputLock.EnterReadLock();
            text = outputBuilder.ToString();
        }
        finally{ outputLock.ExitReadLock();}
        // The output is empty, there are no changes, or the output max length has already been reached => do nothing.
        if (text is not { Length: > 0 } o ||
            o.Length == output.Message.Length ||
            output.Message.Length >= MaxOutputLength)
        {
            return false; // No changes
        }
        // Executable output and error messages can be significantly long. Handle super long messages here.
        output.Message = o[..Math.Min(MaxOutputLength, o.Length)];
        if (output.Message.Length >= MaxOutputLength)
        {
            attempt.AddOutput($"Output has been truncated to first {MaxOutputLength} characters.",
                insertFirst: true);
        }
        return true; // Changes were made
    }

    private static bool UpdateErrors(ExeStepExecutionAttempt attempt, ErrorMessage error,
        StringBuilder errorBuilder, ReaderWriterLockSlim errorLock)
    {
        string text;
        try
        {
            errorLock.EnterReadLock();
            text = errorBuilder.ToString();
        }
        finally { errorLock.ExitReadLock(); }
        
        // The error is empty, there are no changes, or the error max length has already been reached => do nothing.
        if (text is not { Length: > 0 } e ||
            e.Length == error.Message.Length ||
            error.Message.Length >= MaxOutputLength)
        {
            return false; // No changes
        }
        error.Message = e[..Math.Min(MaxOutputLength, e.Length)];
        if (error.Message.Length >= MaxOutputLength)
        {
            attempt.AddError(null, $"Error output has been truncated to first {MaxOutputLength} characters.",
                insertFirst: true);
        }
        return true; // Changes were made
    }
    
    private async Task UpdateOutputToDbAsync(ExeStepExecutionAttempt attempt, InfoMessage output,
        IReadOnlyList<StringBuilder> outputBuilders, ReaderWriterLockSlim outputLock, CancellationToken cancellationToken)
    {
        try
        {
            if (outputBuilders is not [var outputBuilder, ..] || !UpdateOutput(attempt, output, outputBuilder, outputLock))
                return;
            
            await using var dbContext = await _dbContextFactory.CreateDbContextAsync(cancellationToken);
            await dbContext.StepExecutionAttempts
                .Where(x => x.ExecutionId == attempt.ExecutionId &&
                            x.StepId == attempt.StepId &&
                            x.RetryAttemptIndex == attempt.RetryAttemptIndex)
                .ExecuteUpdateAsync(
                    // The output InfoMessage should be included in the InfoMessages collection.
                    x => x.SetProperty(p => p.InfoMessages, attempt.InfoMessages),
                    cancellationToken: cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating output for step");
        }
    }
    
    private async Task UpdateErrorsToDbAsync(ExeStepExecutionAttempt attempt, ErrorMessage error,
        IReadOnlyList<StringBuilder> errorBuilders, ReaderWriterLockSlim errorLock, CancellationToken cancellationToken)
    {
        try
        {
            if (errorBuilders is not [var errorBuilder, ..] || !UpdateErrors(attempt, error, errorBuilder, errorLock))
                return;
            
            await using var dbContext = await _dbContextFactory.CreateDbContextAsync(cancellationToken);
            await dbContext.StepExecutionAttempts
                .Where(x => x.ExecutionId == attempt.ExecutionId &&
                            x.StepId == attempt.StepId &&
                            x.RetryAttemptIndex == attempt.RetryAttemptIndex)
                .ExecuteUpdateAsync(
                    x => x.SetProperty(p => p.ErrorMessages, attempt.ErrorMessages),
                    cancellationToken: cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating error output for step");
        }
    }
}
