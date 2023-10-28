using Biflow.DataAccess.Models;
using Biflow.Executor.Core.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Biflow.Executor.Core.StepExecutor;

internal class FunctionStepExecutor(
    ILogger<FunctionStepExecutor> logger,
    IDbContextFactory<ExecutorDbContext> dbContextFactory,
    IHttpClientFactory httpClientFactory,
    FunctionStepExecution step) : FunctionStepExecutorBase(logger, dbContextFactory, step)
{
    private readonly IHttpClientFactory _httpClientFactory = httpClientFactory;

    protected override async Task<Result> ExecuteAsync(ExtendedCancellationTokenSource cancellationTokenSource)
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

            var request = await BuildFunctionInvokeRequestAsync(cancellationToken);

            // A regular httpTrigger function can run for several minutes. Use an HttpClient with no timeout for httpTrigger functions.
            var noTimeoutClient = _httpClientFactory.CreateClient("notimeout");

            // Send the request to the function url. This will start the function, if the request was successful.
            response = await noTimeoutClient.SendAsync(request, linkedCts.Token);
            content = await response.Content.ReadAsStringAsync(CancellationToken.None);
            AddOutput(content);
        }
        catch (OperationCanceledException ex)
        {
            if (timeoutCts.IsCancellationRequested)
            {
                AddError(ex, "Step execution timed out");
                return Result.Failure;
            }
            AddWarning(ex);
            return Result.Cancel;
        }
        catch (Exception ex)
        {
            AddError(ex, "Error sending POST request to invoke function");
            return Result.Failure;
        }

        try
        {
            response.EnsureSuccessStatusCode();
            return Result.Success;
        }
        catch (Exception ex)
        {
            AddError(ex, "Function execution failed");
            return Result.Failure;
        }
    }

}
