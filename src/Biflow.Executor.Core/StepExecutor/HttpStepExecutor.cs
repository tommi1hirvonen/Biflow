using System.Net;
using System.Text;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Graph;

namespace Biflow.Executor.Core.StepExecutor;

[UsedImplicitly]
internal class HttpStepExecutor(
    ILogger<HttpStepExecutor> logger,
    IDbContextFactory<ExecutorDbContext> dbContextFactory,
    IOptionsMonitor<ExecutionOptions> options,
    IHttpClientFactory httpClientFactory)
    : StepExecutor<HttpStepExecution, HttpStepExecutionAttempt>(logger, dbContextFactory)
{
    private readonly int _pollingIntervalMs = options.CurrentValue.PollingIntervalMs;

    private static readonly string[] ContentHeaders =
    [
        "Content-Type",
        "Content-Length",
        "Content-Encoding",
        "Content-Language",
        "Content-Location",
        "Content-Disposition",
        "Content-Range",
        "Expires",
        "Last-Modified"
    ];
    
    protected override async Task<Result> ExecuteAsync(
        OrchestrationContext context,
        HttpStepExecution step,
        HttpStepExecutionAttempt attempt,
        ExtendedCancellationTokenSource cancellationTokenSource)
    {
        var cancellationToken = cancellationTokenSource.Token;
        cancellationToken.ThrowIfCancellationRequested();
        
        using var timeoutCts = step.TimeoutMinutes > 0
            ? new CancellationTokenSource(TimeSpan.FromMinutes(step.TimeoutMinutes))
            : new CancellationTokenSource();

        // The linked timeout token will cancel if the timeout expires or the step was canceled manually.
        using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, timeoutCts.Token);
        
        // Use an HttpClient with no timeout.
        // The step timeout setting is used for request timeout via cancellation token. 
        var noTimeoutClient = httpClientFactory.CreateClient("notimeout");
        
        HttpResponseMessage? response = null;
        try
        {
            using var request = BuildRequest(step, attempt);
            response = await noTimeoutClient.SendAsync(request, linkedCts.Token);
            attempt.AddOutput($"Response status code: {(int)response.StatusCode} {response.StatusCode}");
            var content = await response.Content.ReadAsStringAsync(CancellationToken.None);
            if (!string.IsNullOrEmpty(content))
                attempt.AddOutput($"Response content:\n{content}");
        }
        catch (OperationCanceledException ex)
        {
            response?.Dispose();
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
            response?.Dispose();
            attempt.AddError(ex, "Error building/sending HTTP request");
            return Result.Failure;
        }

        try
        {
            response.EnsureSuccessStatusCode();
        }
        catch (Exception ex)
        {
            response.Dispose();
            attempt.AddError(ex, "HTTP response reported error status code");
            return Result.Failure;
        }

        if (step.DisableAsyncPattern)
        {
            response.Dispose();
            return Result.Success;
        }
        
        try
        {
            return await PollAsyncPatternAsync(attempt, noTimeoutClient, response, linkedCts.Token);
        }
        catch (Exception ex)
        {
            attempt.AddError(ex, "Error polling for async pattern");
            return Result.Failure;
        }
        finally
        {
            response.Dispose();
        }
    }

    private static HttpRequestMessage BuildRequest(
        HttpStepExecution step,
        HttpStepExecutionAttempt attempt)
    {
        var method = step.Method switch
        {
            HttpStepMethod.Get => HttpMethod.Get,
            HttpStepMethod.Post => HttpMethod.Post,
            HttpStepMethod.Put => HttpMethod.Put,
            HttpStepMethod.Delete => HttpMethod.Delete,
            HttpStepMethod.Patch => HttpMethod.Patch,
            _ => throw new ArgumentOutOfRangeException($"Unhandled HTTP method: {step.Method}")
        };
        var parameters = step.StepExecutionParameters.ToStringDictionary();
        var url = step.Url.Replace(parameters);
        // Only log the evaluated URL if it differs from the original URL. 
        if (url != step.Url) attempt.AddOutput($"Evaluated URL:\n{url}");
        var message = new HttpRequestMessage(method, url);

        var headers = step.Headers
            .Select(h => new HttpHeader(h.Key, h.Value.Replace(parameters)))
            .ToArray();
        // Only log the evaluated headers if they differ from the original headers.
        if (headers.Length > 0 && headers.Any(h1 => step.Headers.Any(h2 => h1.Key == h2.Key && h1.Value != h2.Value)))
        {
            var headersAsText = string.Join("\n", headers.Select(h => $"{h.Key}: {h.Value}"));
            attempt.AddOutput($"Evaluated headers:\n{headersAsText}");
        }
        // Exclude content headers from the request message headers.
        foreach (var header in headers.Where(h => !ContentHeaders.Contains(h.Key)))
        {
            message.Headers.Add(header.Key, header.Value);
        }
        
        // If a request body was defined, add it to the request content.
        var input = step.Body?.Replace(parameters);
        if (input != step.Body) attempt.AddOutput($"Evaluated request body:\n{input}");
        var bytes = Encoding.UTF8.GetBytes(input ?? string.Empty);
        // Use ByteArrayContent since it doesn't have a Content-Type header.
        message.Content = new ByteArrayContent(bytes);
        // Only include content headers in the request message content headers.
        foreach (var header in headers.Where(h => ContentHeaders.Contains(h.Key)))
        {
            message.Content.Headers.Add(header.Key, header.Value);
        }
        return message;
    }

    private async Task<Result> PollAsyncPatternAsync(
        HttpStepExecutionAttempt attempt,
        HttpClient client,
        HttpResponseMessage initialResponse,
        CancellationToken cancellationToken)
    {
        if (initialResponse.StatusCode != HttpStatusCode.Accepted ||
            initialResponse.Headers.Location is not { AbsoluteUri: { Length: > 0 } url })
        {
            return Result.Success;
        }

        attempt.AddOutput($"Polling URL:\n{url}");
        using var response = await PollAndGetResponseAsync(client, url, cancellationToken);
        
        attempt.AddOutput($"Final polling response status code: {(int)response.StatusCode} {response.StatusCode}");
        var content = await response.Content.ReadAsStringAsync(cancellationToken);
        if (!string.IsNullOrEmpty(content))
        {
            attempt.AddOutput($"Final polling response content:\n{content}");
        }

        if (response.IsSuccessStatusCode)
        {
            return Result.Success;
        }
        
        attempt.AddError("Polling response reported error status code");
        return Result.Failure;
    }
    
    private async Task<HttpResponseMessage> PollAndGetResponseAsync(
        HttpClient client,
        string url,
        CancellationToken cancellationToken)
    {
        while (true)
        {
            await Task.Delay(_pollingIntervalMs, cancellationToken);
            var response = await client.GetAsync(url, cancellationToken);
            if (response.StatusCode != HttpStatusCode.Accepted)
                return response;
            response.Dispose();
        }
    }
}
