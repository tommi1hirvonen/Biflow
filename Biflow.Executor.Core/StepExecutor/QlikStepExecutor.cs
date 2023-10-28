using Biflow.DataAccess.Models;
using Biflow.Executor.Core.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;

namespace Biflow.Executor.Core.StepExecutor;

internal class QlikStepExecutor : StepExecutorBase
{
    private readonly ILogger<QlikStepExecutor> _logger;
    private readonly IDbContextFactory<ExecutorDbContext> _dbContextFactory;
    private readonly HttpClient _httpClient;
    private readonly QlikStepExecution _step;
    private readonly int _pollingIntervalMs;
    private readonly JsonSerializerOptions _deserializerOptions = new() { PropertyNameCaseInsensitive = true };

    public QlikStepExecutor(
        ILogger<QlikStepExecutor> logger,
        IDbContextFactory<ExecutorDbContext> dbContextFactory,
        IOptionsMonitor<ExecutionOptions> options,
        IHttpClientFactory httpClientFactory,
        QlikStepExecution stepExecution) : base(logger, dbContextFactory, stepExecution)
    {
        _logger = logger;
        _dbContextFactory = dbContextFactory;
        _step = stepExecution;
        _pollingIntervalMs = options.CurrentValue.PollingIntervalMs;
        _httpClient = httpClientFactory.CreateClient();
        _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _step.QlikCloudClient.ApiToken);
    }

    protected override async Task<Result> ExecuteAsync(ExtendedCancellationTokenSource cancellationTokenSource)
    {
        var cancellationToken = cancellationTokenSource.Token;
        cancellationToken.ThrowIfCancellationRequested();

        // Start app reload.
        Reload reload;
        try
        {
            var postReloadUrl = $"{_step.QlikCloudClient.EnvironmentUrl}/api/v1/reloads";
            var message = new
            {
                appId = _step.AppId,
                partial = false
            };
            var response = await _httpClient.PostAsJsonAsync(postReloadUrl, message, cancellationToken);
            response.EnsureSuccessStatusCode();
            var responseBody = await response.Content.ReadAsStringAsync();
            ArgumentNullException.ThrowIfNull(responseBody);
            reload = JsonSerializer.Deserialize<Reload>(responseBody, _deserializerOptions)
                ?? throw new ApplicationException("Reload response was null");
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error starting app refresh");
            AddError(ex, "Error starting app reload");
            return new Failure();
        }

        // Create timeout cancellation token source here
        // so that the timeout countdown starts right after the app reload was started.
        using var timeoutCts = _step.TimeoutMinutes > 0
                ? new CancellationTokenSource(TimeSpan.FromMinutes(_step.TimeoutMinutes))
                : new CancellationTokenSource();
        using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, timeoutCts.Token);

        // Update reload id for the step execution attempt
        try
        {
            using var context = await _dbContextFactory.CreateDbContextAsync();
            var attempt = _step.StepExecutionAttempts.MaxBy(e => e.RetryAttemptIndex);
            if (attempt is not null && attempt is QlikStepExecutionAttempt qlik)
            {
                qlik.ReloadId = reload.Id;
                context.Attach(qlik);
                context.Entry(qlik).Property(e => e.ReloadId).IsModified = true;
                await context.SaveChangesAsync(CancellationToken.None);
            }
            else
            {
                throw new InvalidOperationException("Could not find step execution attempt to update app reload id");
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "{ExecutionId} {Step} Error updating app reload id", _step.ExecutionId, _step);
            AddWarning(ex, $"Error updating app reload id {reload.Id}");
        }

        var getReloadUrl = $"{_step.QlikCloudClient.EnvironmentUrl}/api/v1/reloads/{reload.Id}";
        while (true)
        {
            try
            {
                await Task.Delay(_pollingIntervalMs, linkedCts.Token);
                reload = await _httpClient.GetFromJsonAsync<Reload>(getReloadUrl, linkedCts.Token)
                    ?? throw new ApplicationException("Reload response was null");

                if (reload is { Status: "SUCCEEDED" })
                {
                    AddOutput(reload.Log);
                    return new Success();
                }
                else if (reload is { Status: "FAILED" or "CANCELED" or "EXCEEDED_LIMIT" })
                {
                    AddOutput(reload.Log);
                    AddError($"Reload reported status {reload.Status}");
                    return new Failure();
                }
                // Reload not finished => iterate again
            }
            catch (OperationCanceledException ex)
            {
                var reason = timeoutCts.IsCancellationRequested ? "StepTimedOut" : "StepWasCanceled";
                await CancelAsync(reload.Id);
                if (timeoutCts.IsCancellationRequested)
                {
                    AddError(ex, "Step execution timed out");
                    return new Failure();
                }
                AddWarning(ex);
                return new Cancel();
            }
            catch (Exception ex)
            {
                AddError(ex, "Error getting reload status");
                return new Failure();
            }
        }
    }

    private async Task CancelAsync(string reloadId)
    {
        try
        {
            var cancelUrl = $"{_step.QlikCloudClient.EnvironmentUrl}/api/v1/reloads/{reloadId}/actions/cancel";
            var response = await _httpClient.PostAsync(cancelUrl, null);
            response.EnsureSuccessStatusCode();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "{ExecutionId} {Step} Error canceling reload", _step.ExecutionId, _step);
            AddWarning(ex, "Error canceling reload");
        }
    }

    private record Reload(string Id, string Status, string? Log);
}
