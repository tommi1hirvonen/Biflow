using System.Collections.Frozen;
using System.Net;
using System.Text;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Biflow.Executor.Core.StepExecutor;

internal class HttpStepExecutor(
    IServiceProvider serviceProvider,
    HttpStepExecution step,
    HttpStepExecutionAttempt attempt) : IStepExecutor
{
    private readonly ILogger<HttpStepExecutor> _logger = serviceProvider
        .GetRequiredService<ILogger<HttpStepExecutor>>();
    private readonly IDbContextFactory<ExecutorDbContext> _dbContextFactory = serviceProvider
        .GetRequiredService<IDbContextFactory<ExecutorDbContext>>();
    private readonly int _pollingIntervalMs = serviceProvider
        .GetRequiredService<IOptionsMonitor<ExecutionOptions>>()
        .CurrentValue
        .PollingIntervalMs;
    // Use an HttpClient with no timeout.
    // The step timeout setting is used for request timeout via cancellation token. 
    private readonly HttpClient _client = serviceProvider
        .GetRequiredService<IHttpClientFactory>()
        .CreateClient("notimeout");

    private static readonly FrozenSet<string> ContentHeaders =
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
    
    public async Task<Result> ExecuteAsync(OrchestrationContext context, CancellationContext cancellationContext)
    {
        var cancellationToken = cancellationContext.CancellationToken;
        cancellationToken.ThrowIfCancellationRequested();
        
        using var timeoutCts = step.TimeoutMinutes > 0
            ? new CancellationTokenSource(TimeSpan.FromMinutes(step.TimeoutMinutes))
            : new CancellationTokenSource();

        // The linked timeout token will cancel if the timeout expires or the step was canceled manually.
        using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, timeoutCts.Token);
        
        HttpResponseMessage? response = null;
        try
        {
            try
            {
                using var request = BuildRequest();
                response = await _client.SendAsync(request, linkedCts.Token);
                attempt.AddOutput($"Response status code: {(int)response.StatusCode} {response.StatusCode}");
                var content = await response.Content.ReadAsStringAsync(CancellationToken.None);
                if (!string.IsNullOrEmpty(content))
                    attempt.AddOutput($"Response content:\n{content}");
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
                attempt.AddError(ex, "Error building/sending HTTP request");
                return Result.Failure;
            }

            try
            {
                response.EnsureSuccessStatusCode();
            }
            catch (Exception ex)
            {
                attempt.AddError(ex, "HTTP response reported error status code");
                return Result.Failure;
            }

            if (step.DisableAsyncPattern)
            {
                return Result.Success;
            }

            try
            {
                return await PollAsyncPatternAsync(response, linkedCts.Token);
            }
            catch (Exception ex)
            {
                attempt.AddError(ex, "Error polling for async pattern");
                return Result.Failure;
            }
        }
        finally
        {
            response?.Dispose();
        }
    }

    private HttpRequestMessage BuildRequest()
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

    private async Task<Result> PollAsyncPatternAsync(HttpResponseMessage initialResponse,
        CancellationToken cancellationToken)
    {
        if (initialResponse.StatusCode != HttpStatusCode.Accepted ||
            initialResponse.Headers.Location is not { AbsoluteUri: { Length: > 0 } url })
        {
            return Result.Success;
        }

        attempt.AddOutput($"Polling URL:\n{url}");
        
        // Update output, which by now contains response headers, content and location header URL.
        try
        {
            await UpdateOutputToDbAsync(CancellationToken.None);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating output for step");
            attempt.AddWarning(ex, "Error updating step output");
        }
        
        using var response = await PollAndGetResponseAsync(url, cancellationToken);
        
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
    
    private async Task<HttpResponseMessage> PollAndGetResponseAsync(string url, CancellationToken cancellationToken)
    {
        while (true)
        {
            await Task.Delay(_pollingIntervalMs, cancellationToken);
            var response = await _client.GetAsync(url, cancellationToken);
            if (response.StatusCode != HttpStatusCode.Accepted)
                return response;
            response.Dispose();
        }
    }
    
    private async Task UpdateOutputToDbAsync(CancellationToken cancellationToken)
    {
        await using var dbContext = await _dbContextFactory.CreateDbContextAsync(cancellationToken);
        await dbContext.StepExecutionAttempts
            .Where(x => x.ExecutionId == attempt.ExecutionId &&
                        x.StepId == attempt.StepId &&
                        x.RetryAttemptIndex == attempt.RetryAttemptIndex)
            .ExecuteUpdateAsync(
                x => x.SetProperty(p => p.InfoMessages, attempt.InfoMessages),
                cancellationToken: cancellationToken);
    }
}
