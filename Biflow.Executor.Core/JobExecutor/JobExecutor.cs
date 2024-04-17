using Biflow.Executor.Core.ExecutionValidation;
using Biflow.Executor.Core.JobOrchestrator;
using Biflow.Executor.Core.Notification;
using Microsoft.Extensions.Logging;
using System.Diagnostics;

namespace Biflow.Executor.Core.JobExecutor;

internal partial class JobExecutor(
    ILogger<JobExecutor> logger,
    IEnumerable<IExecutionValidator> validators,
    IDbContextFactory<ExecutorDbContext> dbContextFactory,
    INotificationService notificationService,
    IJobOrchestratorFactory jobOrchestratorFactory,
    Execution execution) : IJobExecutor
{
    private readonly ILogger<JobExecutor> _logger = logger;
    private readonly IEnumerable<IExecutionValidator> _validators = validators;
    private readonly IDbContextFactory<ExecutorDbContext> _dbContextFactory = dbContextFactory;
    private readonly INotificationService _notificationService = notificationService;
    private readonly IJobOrchestrator _jobOrchestrator = jobOrchestratorFactory.Create(execution);
    private readonly Execution _execution = execution;

    public Execution Execution => _execution;

    public async Task RunAsync(Guid executionId, CancellationToken cancellationToken)
    {
        // CancellationToken is triggered when the executor service is being shut down
        cancellationToken.ThrowIfCancellationRequested();

        try
        {
            var process = Process.GetCurrentProcess();
            using var context = _dbContextFactory.CreateDbContext();
            context.Attach(_execution);
            _execution.ExecutorProcessId = process.Id;
            _execution.ExecutionStatus = ExecutionStatus.Running;
            _execution.StartedOn = DateTimeOffset.Now;
            await context.SaveChangesAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating job execution status");
            return;
        }

        // Run execution validations (circular dependencies etc.)
        foreach (var validator in _validators)
        {
            var result = await validator.ValidateAsync(
                execution: _execution,
                onValidationFailed: (message) => UpdateExecutionFailedAsync($"Execution failed validation. {message}"),
                cancellationToken);

            if (!result)
            {
                return;
            }
        }

        // Update execution parameter values for parameters that use expressions.
        try
        {
            using var context = await _dbContextFactory.CreateDbContextAsync(cancellationToken);
            foreach (var parameter in _execution.ExecutionParameters.Where(p => p.UseExpression))
            {
                context.Attach(parameter);
                parameter.ParameterValue = new(await parameter.EvaluateAsync());
            }
            await context.SaveChangesAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "{executionId} Error evaluating execution parameters and saving evaluation results", executionId);
            await UpdateExecutionFailedAsync("Error evaluating execution parameters and saving evaluation results");
            return;
        }

        try
        {
            // Don't pass cancellation token to notification task to not needlessly send notifications
            // if service is being shut down. Instead, pass new token which can be canceled if timeout is not reached.
            using var notificationCts = new CancellationTokenSource();
            var notificationTask = _execution.OvertimeNotificationLimitMinutes > 0
                ? Task.Delay(TimeSpan.FromMinutes(_execution.OvertimeNotificationLimitMinutes), notificationCts.Token)
                : Task.Delay(-1, notificationCts.Token); // infinite timeout

            var orchestrationTask = _jobOrchestrator.RunAsync(cancellationToken);

            await Task.WhenAny(notificationTask, orchestrationTask);

            // If the notification task completed first, send long running notification.
            if (notificationTask.IsCompleted)
            {
                await _notificationService.SendLongRunningExecutionNotificationAsync(_execution);
            }
            notificationCts.Cancel();
            await orchestrationTask; // Wait for orchestration to finish.
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during job execution");
        }

        // Update job execution status.
        try
        {
            using var context = _dbContextFactory.CreateDbContext();
            context.Attach(_execution);
            _execution.ExecutionStatus = _execution.GetCalculatedStatus();
            _execution.EndedOn = DateTimeOffset.Now;
            await context.SaveChangesAsync(CancellationToken.None);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating execution status");
        }

        // In case of cancellation (service shutdown), do not send notifications.
        cancellationToken.ThrowIfCancellationRequested();

        await _notificationService.SendCompletionNotificationAsync(_execution);
    }

    public void Cancel(string username) => _jobOrchestrator?.CancelExecution(username);

    public void Cancel(string username, Guid stepId) => _jobOrchestrator?.CancelExecution(username, stepId);

    private async Task UpdateExecutionFailedAsync(string errorMessage)
    {
        using var context = _dbContextFactory.CreateDbContext();
        
        foreach (var attempt in _execution.StepExecutions.SelectMany(s => s.StepExecutionAttempts))
        {
            context.Attach(attempt);
            attempt.StartedOn = DateTimeOffset.Now;
            attempt.EndedOn = DateTimeOffset.Now;
            attempt.ExecutionStatus = StepExecutionStatus.Failed;
            attempt.AddError(errorMessage);
        }

        context.Attach(_execution);
        _execution.StartedOn = DateTimeOffset.Now;
        _execution.EndedOn = DateTimeOffset.Now;
        _execution.ExecutionStatus = ExecutionStatus.Failed;
        // Do not cancel saving failed status.
        await context.SaveChangesAsync();
    }
}