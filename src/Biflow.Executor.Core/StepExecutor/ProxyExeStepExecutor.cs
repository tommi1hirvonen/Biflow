using System.Net.Http.Json;
using System.Text.Json;
using System.Threading.Channels;
using Biflow.ExecutorProxy.Core;
using Microsoft.Extensions.Logging;

namespace Biflow.Executor.Core.StepExecutor;

internal class ProxyExeStepExecutor : IStepExecutor
{
    private const int MaxOutputLength = 500_000;
    
    private readonly InfoMessage _outputMessage = new("");
    private readonly ErrorMessage _errorMessage = new("", null);
    // Create unbounded channels. These are used in conjunction with LIFO consumers to update the latest message.
    private readonly Channel<(string Text, bool IsTruncated)> _outputChannel =
        Channel.CreateUnbounded<(string Text, bool IsTruncated)>();
    private readonly Channel<(string Text, string? StackTrace, bool IsTruncated)> _errorChannel =
        Channel.CreateUnbounded<(string Text, string? StackTrace, bool IsTruncated)>();

    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<ProxyExeStepExecutor> _logger;
    private readonly IDbContextFactory<ExecutorDbContext> _dbContextFactory;
    private readonly ExeStepExecution _step;
    private readonly ExeStepExecutionAttempt _attempt;
    private readonly Proxy _proxy;

    public ProxyExeStepExecutor(IHttpClientFactory httpClientFactory,
        ILogger<ProxyExeStepExecutor> logger,
        IDbContextFactory<ExecutorDbContext> dbContextFactory,
        ExeStepExecution step,
        ExeStepExecutionAttempt attempt,
        Proxy proxy)
    {
        _httpClientFactory = httpClientFactory;
        _logger = logger;
        _dbContextFactory = dbContextFactory;
        _step = step;
        _attempt = attempt;
        _proxy = proxy;
        // Store messages in the attempt because the messages themselves will be updated periodically.
        _attempt.InfoMessages.Insert(0, _outputMessage);
        _attempt.ErrorMessages.Insert(0, _errorMessage);
    }

    public async Task<Result> ExecuteAsync(OrchestrationContext context, ExtendedCancellationTokenSource cts)
    {
        var cancellationToken = cts.Token;
        cancellationToken.ThrowIfCancellationRequested();
        
        var client = CreateProxyHttpClient();
        var request = CreateExeProxyRunRequest();

        using var response = await client.PostAsJsonAsync("/exe", request, cancellationToken);
        await using var contentStream = await response.Content.ReadAsStreamAsync(cancellationToken);
        var taskStartedResponse = JsonSerializer.Deserialize<TaskStartedResponse>(
            contentStream,
            JsonSerializerOptions.Web);

        if (taskStartedResponse is null)
        {
            _logger.LogError("{ExecutionId} {Step} Error starting remote execution, no task id was returned",
                _step.ExecutionId, _step);
            _attempt.AddError("No task id was returned from the proxy when starting remote execution.");
            return Result.Failure;
        }
        
        using var timeoutCts = _step.TimeoutMinutes > 0
            ? new CancellationTokenSource(TimeSpan.FromMinutes(_step.TimeoutMinutes))
            : new CancellationTokenSource();
        
        using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, timeoutCts.Token);

        // Create periodic consumers to update info and error messages while the process is still running.
        // This way we can push updates to the database even if the process has not yet finished.
        // This can be useful in scenarios where the process is long-running and the user wants to see the progress.
        using var outputConsumer = CreateOutputConsumer();
        var outputConsumerTask = outputConsumer.StartConsumingAsync(linkedCts.Token);

        using var errorConsumer = CreateErrorConsumer();
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
                if (processId != _attempt.ExeProcessId)
                {
                    _attempt.ExeProcessId = processId;
                    await UpdateProcessIdAsync();
                }
                _ = _outputChannel.Writer.TryWrite((output ?? "", outputTruncated));
                _ = _errorChannel.Writer.TryWrite((error ?? "", stackTrace, errorTruncated));
            } while (status is ExeTaskRunningResponse);

            switch (status)
            {
                case null:
                    _logger.LogError(
                        "{ExecutionId} {Step} Error getting remote execution status, no status was returned",
                        _step.ExecutionId, _step);
                    _attempt.AddError("No status was returned from the proxy when getting remote execution status.");
                    return Result.Failure;
                case ExeTaskCompletedResponse completed:
                    if (_step.ExeSuccessExitCode is { } successExitCode)
                    {
                        return completed.ExitCode == successExitCode ? Result.Success : Result.Failure;
                    }
                    return Result.Success;
                case ExeTaskFailedResponse failed:
                    _attempt.AddError("Remote execution failed with an internal error.");
                    _attempt.AddError(failed.ErrorMessage);
                    return Result.Failure;
                default:
                    _attempt.AddError(
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
                _logger.LogError(ex, "{ExecutionId} {Step} Error canceling remote execution after timeout",
                    _step.ExecutionId,
                    _step);
                _attempt.AddWarning(ex, "Error canceling remote execution after timeout");
            }

            if (timeoutCts.IsCancellationRequested)
            {
                _attempt.AddError(cancelEx, "Executing remote executable timed out");
                return Result.Failure;
            }

            _attempt.AddWarning(cancelEx);
            return Result.Cancel;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "{ExecutionId} {Step} Error while executing remote executable {FileName}",
                _step.ExecutionId,
                _step,
                _step.ExeFileName);
            _attempt.AddError(ex, "Error while executing remote executable");
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
                _ = UpdateOutput(output ?? "", outputTruncated);
                _ = UpdateErrors(error ?? "", stackTrace, errorTruncated);
            }
            catch (Exception ex) { _logger.LogError(ex, "Error updating final output and error messages"); }
            await outputConsumerTask;
            await errorConsumerTask;
        }
    }
    
    private HttpClient CreateProxyHttpClient()
    {
        var client = _httpClientFactory.CreateClient();
        client.BaseAddress = new Uri(_proxy.ProxyUrl);
        if (_proxy.ApiKey is not null)
        {
            client.DefaultRequestHeaders.Add("x-api-key", _proxy.ApiKey);
        }
        return client;
    }

    private ExeProxyRunRequest CreateExeProxyRunRequest()
    {
        string? arguments;
        if (!string.IsNullOrWhiteSpace(_step.ExeArguments))
        {
            var parameters = _step.StepExecutionParameters.ToStringDictionary();
            arguments = _step.ExeArguments.Replace(parameters);
        }
        else
        {
            arguments = null;
        }

        var workingDirectory = !string.IsNullOrWhiteSpace(_step.ExeWorkingDirectory)
            ? _step.ExeWorkingDirectory
            : null;
        var request = new ExeProxyRunRequest
        {
            ExePath = _step.ExeFileName,
            Arguments = arguments,
            WorkingDirectory = workingDirectory
        };
        return request;
    }

    private PeriodicChannelConsumer<(string Text, bool IsTruncated)> CreateOutputConsumer() => new(
        logger: _logger,
        reader: _outputChannel.Reader,
        // Update every 10 seconds for the first 5 minutes (300 sec), then every 30 seconds.
        interval: iteration => iteration <= 30 ? TimeSpan.FromSeconds(10) : TimeSpan.FromSeconds(30),
        bufferPublished: (buffer, ct) =>
        {
            if (buffer is not [.., var (text, isTruncated)] || !UpdateOutput(text, isTruncated))
                return Task.CompletedTask;
            return UpdateOutputToDbAsync(ct);
        },
        // Since we are consuming entire messages from the proxy API instead of an internal string builder,
        // enable LIFO so that the last message is always processed.
        enableLastInFirstOut: true,
        bufferCapacity: 1);

    private PeriodicChannelConsumer<(string Text, string? StackTrace, bool IsTruncated)> CreateErrorConsumer() => new(
        logger: _logger,
        reader: _errorChannel.Reader,
        interval: iteration => iteration <= 30 ? TimeSpan.FromSeconds(10) : TimeSpan.FromSeconds(30),
        bufferPublished: (buffer, ct) =>
        {
            if (buffer is not [.., var (text, stackTrace, isTruncated)] || !UpdateErrors(text, stackTrace, isTruncated))
                return Task.CompletedTask;
            return UpdateErrorsToDbAsync(ct);
        },
        enableLastInFirstOut: true,
        bufferCapacity: 1);
    
    private async Task UpdateProcessIdAsync()
    {
        try
        {
            await using var dbContext = await _dbContextFactory.CreateDbContextAsync();
            await dbContext.Set<ExeStepExecutionAttempt>()
                .Where(x => x.ExecutionId == _attempt.ExecutionId &&
                            x.StepId == _attempt.StepId &&
                            x.RetryAttemptIndex == _attempt.RetryAttemptIndex)
                .ExecuteUpdateAsync(x => x.SetProperty(p => p.ExeProcessId, _attempt.ExeProcessId));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "{ExecutionId} {Step} Error logging child process id", _attempt.ExecutionId, _step);
            _attempt.AddWarning(ex, "Error logging child process id");
        }
    }
    
    private bool UpdateOutput(string text, bool isTruncated)
    {
        var other = new InfoMessage(text, isTruncated);
        if (_outputMessage.Equals(other)) return false;
        
        _outputMessage.Message = text[..Math.Min(MaxOutputLength, text.Length)];
        _outputMessage.IsTruncated = text.Length > _outputMessage.Message.Length || isTruncated;
        return true;
    }
    
    private bool UpdateErrors(string text, string? stackTrace, bool isTruncated)
    {
        var other = new ErrorMessage(text, stackTrace, isTruncated);
        if (_errorMessage.Equals(other)) return false;
        
        _errorMessage.Message = text[..Math.Min(MaxOutputLength, text.Length)];
        _errorMessage.Exception = stackTrace;
        _errorMessage.IsTruncated = text.Length > _errorMessage.Message.Length || isTruncated;
        return true;
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
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating error output for step");
        }
    }

    public void Dispose()
    {
    }
}