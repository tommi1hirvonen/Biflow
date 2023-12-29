using Biflow.DataAccess.Models;
using Biflow.Executor.Core.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;

namespace Biflow.Executor.Core.StepExecutor;

internal class QlikStepExecutor : IStepExecutor<QlikStepExecutionAttempt>
{
    private readonly ILogger<QlikStepExecutor> _logger;
    private readonly IDbContextFactory<ExecutorDbContext> _dbContextFactory;
    private readonly HttpClient _httpClient;
    private readonly QlikStepExecution _step;
    private readonly QlikCloudClient _client;
    private readonly int _pollingIntervalMs;
    private readonly JsonSerializerOptions _deserializerOptions = new() { PropertyNameCaseInsensitive = true };

    public QlikStepExecutor(
        ILogger<QlikStepExecutor> logger,
        IDbContextFactory<ExecutorDbContext> dbContextFactory,
        IOptionsMonitor<ExecutionOptions> options,
        IHttpClientFactory httpClientFactory,
        QlikStepExecution stepExecution)
    {
        ArgumentNullException.ThrowIfNull(stepExecution.QlikCloudClient);
        _logger = logger;
        _dbContextFactory = dbContextFactory;
        _step = stepExecution;
        _pollingIntervalMs = options.CurrentValue.PollingIntervalMs;
        _httpClient = httpClientFactory.CreateClient();
        _client = stepExecution.QlikCloudClient;
        _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _step.QlikCloudClient.ApiToken);
    }

    public QlikStepExecutionAttempt Clone(QlikStepExecutionAttempt other, int retryAttemptIndex) =>
        new(other, retryAttemptIndex);

    public async Task<Result> ExecuteAsync(QlikStepExecutionAttempt attempt, ExtendedCancellationTokenSource cancellationTokenSource)
    {
        var cancellationToken = cancellationTokenSource.Token;
        cancellationToken.ThrowIfCancellationRequested();

        // Start app reload.
        Reload reload;
        try
        {
            var postReloadUrl = $"{_client.EnvironmentUrl}/api/v1/reloads";
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
            attempt.AddError(ex, "Error starting app reload");
            return Result.Failure;
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
            attempt.ReloadId = reload.Id;
            context.Attach(attempt);
            context.Entry(attempt).Property(e => e.ReloadId).IsModified = true;
            await context.SaveChangesAsync(CancellationToken.None);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "{ExecutionId} {Step} Error updating app reload id", _step.ExecutionId, _step);
            attempt.AddWarning(ex, $"Error updating app reload id {reload.Id}");
        }

        var getReloadUrl = $"{_client.EnvironmentUrl}/api/v1/reloads/{reload.Id}";
        while (true)
        {
            try
            {
                await Task.Delay(_pollingIntervalMs, linkedCts.Token);
                reload = await _httpClient.GetFromJsonAsync<Reload>(getReloadUrl, linkedCts.Token)
                    ?? throw new ApplicationException("Reload response was null");

                if (reload is { Status: "SUCCEEDED" })
                {
                    attempt.AddOutput(reload.Log);
                    return Result.Success;
                }
                else if (reload is { Status: "FAILED" or "CANCELED" or "EXCEEDED_LIMIT" })
                {
                    attempt.AddOutput(reload.Log);
                    attempt.AddError($"Reload reported status {reload.Status}");
                    return Result.Failure;
                }
                // Reload not finished => iterate again
            }
            catch (OperationCanceledException ex)
            {
                var reason = timeoutCts.IsCancellationRequested ? "StepTimedOut" : "StepWasCanceled";
                await CancelAsync(attempt, reload.Id);
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
                attempt.AddError(ex, "Error getting reload status");
                return Result.Failure;
            }
        }
    }

    private async Task CancelAsync(QlikStepExecutionAttempt attempt, string reloadId)
    {
        try
        {
            var cancelUrl = $"{_client.EnvironmentUrl}/api/v1/reloads/{reloadId}/actions/cancel";
            var response = await _httpClient.PostAsync(cancelUrl, null);
            response.EnsureSuccessStatusCode();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "{ExecutionId} {Step} Error canceling reload", _step.ExecutionId, _step);
            attempt.AddWarning(ex, "Error canceling reload");
        }
    }

    private record Reload(string Id, string Status, string? Log);
}
