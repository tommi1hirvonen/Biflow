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
        var (outputChannel, errorChannel) = (Channel.CreateUnbounded<string?>(), Channel.CreateUnbounded<string?>());

        using var process = new Process();
        process.StartInfo = startInfo;
        process.OutputDataReceived += (__, e) => _ = outputChannel.Writer.TryWrite(e.Data);
        process.ErrorDataReceived += (__, e) => _ = errorChannel.Writer.TryWrite(e.Data);

        // Create periodic consumers to update info and error messages while the process is still running.
        // This way we can push updates to the database even if the process has not yet finished.
        // This can be useful in scenarios where the process is long-running and the user wants to see the progress.
        using var outputConsumer = new PeriodicChannelConsumer<string?>(
            logger: _logger,
            reader: outputChannel.Reader,
            interval: TimeSpan.FromSeconds(10),
            bufferPublished: (buffer, ct) => UpdateOutputAsync(attempt, outputMessage, buffer, ct));
        var outputConsumerTask = outputConsumer.StartConsumingAsync(cancellationToken);

        using var errorConsumer = new PeriodicChannelConsumer<string?>(
            logger: _logger,
            reader: errorChannel.Reader,
            interval: TimeSpan.FromSeconds(10),
            bufferPublished: (buffer, ct) => UpdateErrorsAsync(attempt, errorMessage, buffer, ct));
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
            await outputConsumerTask;
            await errorConsumerTask;
        }
    }
    
    private async Task UpdateOutputAsync(ExeStepExecutionAttempt attempt, InfoMessage output,
        IReadOnlyList<string?> buffer, CancellationToken cancellationToken)
    {
        // If the output is already too long, don't bother updating it.
        // The output cannot grow too long outside this method.
        // The truncation warning has thus already been added.
        if (output.Message.Length >= MaxOutputLength)
        {
            return;
        }
        
        try
        {
            // Update the message of the output InfoMessage.
            var outputBuilder = new StringBuilder().Append(output.Message); // Preserve the original message.
            foreach (var message in buffer) outputBuilder.AppendLine(message);
            
            // Executable output and error messages can be significantly long.
            // Handle super long messages here.
            if (outputBuilder.ToString() is { Length: > 0 } o)
            {
                output.Message = o[..Math.Min(MaxOutputLength, o.Length)];
                if (o.Length >= MaxOutputLength)
                {
                    attempt.AddOutput($"Output has been truncated to first {MaxOutputLength} characters.",
                        insertFirst: true);
                }
            }
            else
            {
                return;
            }
            
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
    
    private async Task UpdateErrorsAsync(ExeStepExecutionAttempt attempt, ErrorMessage error,
        IReadOnlyList<string?> buffer, CancellationToken cancellationToken)
    {
        if (error.Message.Length >= MaxOutputLength)
        {
            return;
        }
        
        try
        {
            var errorBuilder = new StringBuilder().Append(error.Message);
            foreach (var message in buffer) errorBuilder.AppendLine(message);
            
            if (errorBuilder.ToString() is { Length: > 0 } o)
            {
                error.Message = o[..Math.Min(MaxOutputLength, o.Length)];
                if (o.Length >= MaxOutputLength)
                {
                    attempt.AddError(null, $"Error output has been truncated to first {MaxOutputLength} characters.",
                        insertFirst: true);
                }
            }
            else
            {
                return;
            }
            
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
}