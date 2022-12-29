using Biflow.DataAccess;
using Biflow.DataAccess.Models;
using Biflow.Executor.Core.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Biflow.Executor.Core.StepExecutor;

internal class FunctionStepExecutor : FunctionStepExecutorBase
{
    private readonly IHttpClientFactory _httpClientFactory;

    public FunctionStepExecutor(
        ILogger<FunctionStepExecutor> logger,
        IDbContextFactory<BiflowContext> dbContextFactory,
        IHttpClientFactory httpClientFactory,
        FunctionStepExecution step)
        : base(logger, dbContextFactory, step)
    {
        _httpClientFactory = httpClientFactory;
    }

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
        }
        catch (OperationCanceledException)
        {
            if (timeoutCts.IsCancellationRequested)
            {
                return Result.Failure("Step execution timed out.", Warning.ToString()); // Report failure => allow possible retries
            }

            throw; // Step was canceled => pass the exception => no retries
        }
        catch (Exception ex)
        {
            return Result.Failure($"Error sending POST request to invoke function:\n{ex.Message}", Warning.ToString());
        }

        try
        {
            response.EnsureSuccessStatusCode();
            return Result.Success(content, Warning.ToString());
        }
        catch (Exception ex)
        {
            return Result.Failure(ex.Message, Warning.ToString(), content);
        }
    }

}
