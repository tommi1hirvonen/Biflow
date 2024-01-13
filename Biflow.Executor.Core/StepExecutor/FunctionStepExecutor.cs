using Biflow.Executor.Core.Common;
using Microsoft.Extensions.Logging;

namespace Biflow.Executor.Core.StepExecutor;

internal class FunctionStepExecutor(
    ILogger<FunctionStepExecutor> logger,
    IDbContextFactory<ExecutorDbContext> dbContextFactory,
    IHttpClientFactory httpClientFactory,
    FunctionStepExecution step)
    : FunctionStepExecutorBase(logger, dbContextFactory, step), IStepExecutor<FunctionStepExecutionAttempt>
{
    private readonly IHttpClientFactory _httpClientFactory = httpClientFactory;

    public FunctionStepExecutionAttempt Clone(FunctionStepExecutionAttempt other, int retryAttemptIndex) =>
        new(other, retryAttemptIndex);

    public async Task<Result> ExecuteAsync(FunctionStepExecutionAttempt attempt, ExtendedCancellationTokenSource cancellationTokenSource)
    {
        var cancellationToken = cancellationTokenSource.Token;
        cancellationToken.ThrowIfCancellationRequested();

        using var timeoutCts = Step.TimeoutMinutes > 0
                    ? new CancellationTokenSource(TimeSpan.FromMinutes(Step.TimeoutMinutes))
                    : new CancellationTokenSource();
        
        HttpResponseMessage response;
        string content;
        try
        {
            // The linked timeout token will cancel if the timeout expires or the step was canceled manually.
            using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, timeoutCts.Token);

            var request = await BuildFunctionInvokeRequestAsync(attempt, cancellationToken);

            // A regular httpTrigger function can run for several minutes. Use an HttpClient with no timeout for httpTrigger functions.
            var noTimeoutClient = _httpClientFactory.CreateClient("notimeout");

            // Send the request to the function url. This will start the function, if the request was successful.
            response = await noTimeoutClient.SendAsync(request, linkedCts.Token);
            content = await response.Content.ReadAsStringAsync(CancellationToken.None);
            attempt.AddOutput(content);
        }
        catch (OperationCanceledException ex)
        {
            if (timeoutCts.IsCancellationRequested)
            {
                attempt.AddError(ex, "Step execution timed out");
                return Result.Failure;
            }
            attempt.AddWarning(ex);
            return Result.Cancel;
        }
        catch (Exception ex)
        {
            attempt.AddError(ex, "Error sending POST request to invoke function");
            return Result.Failure;
        }

        try
        {
            response.EnsureSuccessStatusCode();
            return Result.Success;
        }
        catch (Exception ex)
        {
            attempt.AddError(ex, "Function execution failed");
            return Result.Failure;
        }
    }

}
