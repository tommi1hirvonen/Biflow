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
    IDbContextFactory<ExecutorDbContext> dbContextFactory,
    ExeStepExecution step,
    ExeStepExecutionAttempt attempt) : IStepExecutor
{
    private const int MaxOutputLength = 500_000;

    public Task<Result> ExecuteAsync(OrchestrationContext context, ExtendedCancellationTokenSource cts)
    {
        var cancellationToken = cts.Token;
        cancellationToken.ThrowIfCancellationRequested();

        if (step.GetProxy() is { } proxy)
        {
            return ExecuteRemoteAsync(proxy, cancellationToken);
        }

        return ExecuteLocalAsync(cancellationToken);
    }

    private async Task<Result> ExecuteLocalAsync(CancellationToken cancellationToken)
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
            logger: logger,
            reader: outputChannel.Reader,
            // Update every 10 seconds for the first 5 minutes (300 sec), then every 30 seconds.
            interval: iteration => iteration <= 30 ? TimeSpan.FromSeconds(10) : TimeSpan.FromSeconds(30),
            bufferPublished: async (buffer, ct) =>
            {
                if (buffer is not [var builder, ..] || !UpdateOutput(outputMessage, builder, outputLock))
                    return;
                await UpdateOutputToDbAsync(ct);
            });
        var outputConsumerTask = outputConsumer.StartConsumingAsync(cancellationToken);

        using var errorConsumer = new PeriodicChannelConsumer<StringBuilder>(
            logger: logger,
            reader: errorChannel.Reader,
            interval: iteration => iteration <= 30 ? TimeSpan.FromSeconds(10) : TimeSpan.FromSeconds(30),
            bufferPublished: async (buffer, ct) =>
            {
                if (buffer is not [var builder, ..] || !UpdateErrors(errorMessage, builder, errorLock))
                    return;
                await UpdateErrorsToDbAsync(ct);
            });
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
                logger.LogError(ex, "{ExecutionId} {Step} Error starting process for file name {FileName}",
                    step.ExecutionId, step, step.ExeFileName);
                attempt.AddError(ex, "Error starting process");
                return Result.Failure;
            }

            try
            {
                await using var dbContext = await dbContextFactory.CreateDbContextAsync(CancellationToken.None);
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
                logger.LogError(ex, "{ExecutionId} {Step} Error logging child process id", step.ExecutionId, step);
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
                    logger.LogError(ex, "{ExecutionId} {Step} Error killing process after timeout", step.ExecutionId,
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
                logger.LogError(ex, "{ExecutionId} {Step} Error while executing {FileName}", step.ExecutionId, step,
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
                _ = UpdateOutput(outputMessage, outputBuilder, outputLock);
                _ = UpdateErrors(errorMessage, errorBuilder, errorLock);
            }
            catch (Exception ex) { logger.LogError(ex, "Error updating final output and error messages"); }
            await outputConsumerTask;
            await errorConsumerTask;
        }
    }

    private async Task<Result> ExecuteRemoteAsync(Proxy proxy, CancellationToken cancellationToken)
    {
        var client = CreateProxyHttpClient(proxy);
        var request = CreateExeProxyRunRequest();

        using var response = await client.PostAsJsonAsync("/exe", request, cancellationToken);
        await using var contentStream = await response.Content.ReadAsStreamAsync(cancellationToken);
        var taskStartedResponse = JsonSerializer.Deserialize<TaskStartedResponse>(
            contentStream,
            JsonSerializerOptions.Web);

        if (taskStartedResponse is null)
        {
            logger.LogError("{ExecutionId} {Step} Error starting remote execution, no task id was returned",
                step.ExecutionId, step);
            attempt.AddError("No task id was returned from the proxy when starting remote execution.");
            return Result.Failure;
        }
        
        using var timeoutCts = step.TimeoutMinutes > 0
            ? new CancellationTokenSource(TimeSpan.FromMinutes(step.TimeoutMinutes))
            : new CancellationTokenSource();
        
        using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, timeoutCts.Token);
        
        // Create channels for output and error messages.
        // Store messages in the attempt because the messages themselves will be updated periodically.
        var (outputMessage, errorMessage) = (new InfoMessage(""), new ErrorMessage("", null));
        attempt.InfoMessages.Insert(0, outputMessage);
        attempt.ErrorMessages.Insert(0, errorMessage);
        // Create unbounded channels. These are used in conjunction with LIFO consumers to update the latest message.
        var outputChannel = Channel.CreateUnbounded<(string Text, bool IsTruncated)>();
        var errorChannel = Channel.CreateUnbounded<(string Text, string? StackTrace, bool IsTruncated)>();
        
        using var outputConsumer = new PeriodicChannelConsumer<(string Text, bool IsTruncated)>(
            logger: logger,
            reader: outputChannel.Reader,
            // Update every 10 seconds for the first 5 minutes (300 sec), then every 30 seconds.
            interval: iteration => iteration <= 30 ? TimeSpan.FromSeconds(10) : TimeSpan.FromSeconds(30),
            bufferPublished: async (buffer, ct) =>
            {
                if (buffer is not [.., var (text, isTruncated)] || !UpdateOutput(outputMessage, text, isTruncated))
                    return;
                await UpdateOutputToDbAsync(ct);
            },
            // Since we are consuming entire messages from the proxy API instead of an internal string builder,
            // enable LIFO so that the last message is always processed.
            enableLastInFirstOut: true,
            bufferCapacity: 1);
        var outputConsumerTask = outputConsumer.StartConsumingAsync(linkedCts.Token);

        using var errorConsumer = new PeriodicChannelConsumer<(string Text, string? StackTrace, bool IsTruncated)>(
            logger: logger,
            reader: errorChannel.Reader,
            interval: iteration => iteration <= 30 ? TimeSpan.FromSeconds(10) : TimeSpan.FromSeconds(30),
            bufferPublished: async (buffer, ct) =>
            {
                if (buffer is not [.., var (text, stackTrace, isTruncated)] ||
                    !UpdateErrors(errorMessage, text, stackTrace, isTruncated))
                    return;
                await UpdateErrorsToDbAsync(ct);
            },
            enableLastInFirstOut: true,
            bufferCapacity: 1);
        var errorConsumerTask = errorConsumer.StartConsumingAsync(linkedCts.Token);

        string? output = null, error = null, stackTrace = null;
        bool outputTruncated = false, errorTruncated = false;
        try
        {
            ExeTaskStatusResponse? status;
            do
            {
                await Task.Delay(TimeSpan.FromSeconds(5), linkedCts.Token);

                status = await client.GetFromJsonAsync<ExeTaskStatusResponse>($"/exe/{taskStartedResponse.TaskId}",
                    linkedCts.Token);

                (var processId, output, outputTruncated, error, errorTruncated, stackTrace) = status switch
                {
                    ExeTaskRunningResponse running =>
                        (running.ProcessId, running.Output, running.OutputIsTruncated, running.ErrorOutput,
                            running.ErrorOutputIsTruncated, null),
                    ExeTaskCompletedResponse completed =>
                        (completed.ProcessId, completed.Output, completed.OutputIsTruncated, completed.ErrorOutput,
                            completed.ErrorOutputIsTruncated, completed.InternalError),
                    _ =>
                        (null as int?, null as string, false, null as string, false, null as string)
                };
                if (processId != attempt.ExeProcessId)
                {
                    attempt.ExeProcessId = processId;
                    await UpdateProcessIdAsync();
                }
                _ = outputChannel.Writer.TryWrite((output ?? "", outputTruncated));
                _ = errorChannel.Writer.TryWrite((error ?? "", stackTrace, errorTruncated));
            } while (status is ExeTaskRunningResponse);

            switch (status)
            {
                case null:
                    logger.LogError(
                        "{ExecutionId} {Step} Error getting remote execution status, no status was returned",
                        step.ExecutionId, step);
                    attempt.AddError("No status was returned from the proxy when getting remote execution status.");
                    return Result.Failure;
                case ExeTaskCompletedResponse completed:
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
                    attempt.AddError(
                        $"Unrecognized status {status.GetType().Name} from the proxy when getting remote execution status.");
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
                logger.LogError(ex, "{ExecutionId} {Step} Error canceling remote execution after timeout",
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
            logger.LogError(ex, "{ExecutionId} {Step} Error while executing remote executable {FileName}",
                step.ExecutionId,
                step,
                step.ExeFileName);
            attempt.AddError(ex, "Error while executing remote executable");
            return Result.Failure;
        }
        finally
        {
            outputConsumer.Cancel();
            errorConsumer.Cancel();
            // Update the output and errors one last time in case
            // the periodic consumers did not have a chance to handle the latest updates from the channel.
            try
            {
                _ = UpdateOutput(outputMessage, output ?? "", outputTruncated);
                _ = UpdateErrors(errorMessage, error ?? "", stackTrace, errorTruncated);
            }
            catch (Exception ex) { logger.LogError(ex, "Error updating final output and error messages"); }
            await outputConsumerTask;
            await errorConsumerTask;
        }
    }

    private HttpClient CreateProxyHttpClient(Proxy proxy)
    {
        var client = httpClientFactory.CreateClient();
        client.BaseAddress = new Uri(proxy.ProxyUrl);
        if (proxy.ApiKey is not null)
        {
            client.DefaultRequestHeaders.Add("x-api-key", proxy.ApiKey);
        }
        return client;
    }

    private ExeProxyRunRequest CreateExeProxyRunRequest()
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

    private async Task UpdateProcessIdAsync()
    {
        try
        {
            await using var dbContext = await dbContextFactory.CreateDbContextAsync();
            await dbContext.Set<ExeStepExecutionAttempt>()
                .Where(x => x.ExecutionId == attempt.ExecutionId &&
                            x.StepId == attempt.StepId &&
                            x.RetryAttemptIndex == attempt.RetryAttemptIndex)
                .ExecuteUpdateAsync(x => x.SetProperty(p => p.ExeProcessId, attempt.ExeProcessId));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "{ExecutionId} {Step} Error logging child process id", attempt.ExecutionId, step);
            attempt.AddWarning(ex, "Error logging child process id");
        }
    }

    private static bool UpdateOutput(InfoMessage output, StringBuilder outputBuilder, ReaderWriterLockSlim outputLock)
    {
        string text;
        try
        {
            outputLock.EnterReadLock();
            text = outputBuilder.ToString();
        }
        finally{ outputLock.ExitReadLock();}
        return UpdateOutput(output, text);
    }
    
    private static bool UpdateOutput(InfoMessage output, string text)
    {
        // The output is empty or there are no changes.
        if (string.IsNullOrEmpty(text) || text.Length == output.Message.Length)
        {
            return false; // No changes
        }

        // The output max length has been reached, but the message was not yet marked as truncated.
        if (text.Length >= MaxOutputLength && output.Message.Length >= MaxOutputLength && !output.IsTruncated)
        {
            output.IsTruncated = true;
            return true;
        }
        
        // Executable output and error messages can be significantly long. Handle super long messages here.
        output.Message = text[..Math.Min(MaxOutputLength, text.Length)];
        output.IsTruncated = text.Length > output.Message.Length;
        return true; // Changes were made
    }
    
    private static bool UpdateOutput(InfoMessage output, string text, bool isTruncated)
    {
        var other = new InfoMessage(text, isTruncated);
        if (output.Equals(other)) return false;
        
        output.Message = text[..Math.Min(MaxOutputLength, text.Length)];
        output.IsTruncated = text.Length > output.Message.Length || isTruncated;
        return true;
    }

    private static bool UpdateErrors(ErrorMessage error, StringBuilder errorBuilder, ReaderWriterLockSlim errorLock)
    {
        string text;
        try
        {
            errorLock.EnterReadLock();
            text = errorBuilder.ToString();
        }
        finally { errorLock.ExitReadLock(); }
        return UpdateErrors(error, text);
    }
    
    private static bool UpdateErrors(ErrorMessage error, string text)
    {
        // The error is empty or there are no changes.
        if (string.IsNullOrEmpty(text) || text.Length == error.Message.Length)
        {
            return false; // No changes
        }

        // The error message max length has been reached, but the message was not yet marked as truncated.
        if (text.Length >= MaxOutputLength && error.Message.Length >= MaxOutputLength && !error.IsTruncated)
        {
            error.IsTruncated = true;
            return true;
        }
        
        error.Message = text[..Math.Min(MaxOutputLength, text.Length)];
        error.IsTruncated = text.Length > error.Message.Length;
        return true; // Changes were made
    }
    
    private static bool UpdateErrors(ErrorMessage error, string text, string? stackTrace, bool isTruncated)
    {
        var other = new ErrorMessage(text, stackTrace, isTruncated);
        if (error.Equals(other)) return false;
        
        error.Message = text[..Math.Min(MaxOutputLength, text.Length)];
        error.Exception = stackTrace;
        error.IsTruncated = text.Length > error.Message.Length || isTruncated;
        return true;
    }
    
    private async Task UpdateOutputToDbAsync(CancellationToken cancellationToken)
    {
        try
        {
            await using var dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken);
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
            logger.LogError(ex, "Error updating output for step");
        }
    }
    
    private async Task UpdateErrorsToDbAsync(CancellationToken cancellationToken)
    {
        try
        {
            await using var dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken);
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
            logger.LogError(ex, "Error updating error output for step");
        }
    }

    public void Dispose()
    {
    }
}
