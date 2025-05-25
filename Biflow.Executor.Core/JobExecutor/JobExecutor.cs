using Biflow.Executor.Core.ExecutionValidation;
using Biflow.Executor.Core.JobOrchestrator;
using Biflow.Executor.Core.Notification;
using Microsoft.Extensions.Logging;
using System.Diagnostics;
using Microsoft.Extensions.DependencyInjection;

namespace Biflow.Executor.Core.JobExecutor;

internal class JobExecutor(
    ILogger<JobExecutor> logger,
    [FromKeyedServices(ExecutorServiceKeys.JobExecutorHealthService)]
    HealthService healthService,
    IEnumerable<IExecutionValidator> validators,
    IDbContextFactory<ExecutorDbContext> dbContextFactory,
    INotificationService notificationService,
    IJobOrchestratorFactory jobOrchestratorFactory,
    Execution execution) : IJobExecutor
{
    private readonly ILogger<JobExecutor> _logger = logger;
    private readonly HealthService _healthService = healthService;
    private readonly IEnumerable<IExecutionValidator> _validators = validators;
    private readonly IDbContextFactory<ExecutorDbContext> _dbContextFactory = dbContextFactory;
    private readonly INotificationService _notificationService = notificationService;
    private readonly IJobOrchestrator _jobOrchestrator = jobOrchestratorFactory.Create(execution);

    public Execution Execution { get; } = execution;

    public async Task RunAsync(OrchestrationContext context, CancellationToken cancellationToken)
    {
        // CancellationToken is triggered when the executor service is being shut down
        cancellationToken.ThrowIfCancellationRequested();

        try
        {
            var process = Process.GetCurrentProcess();
            await using var dbContext = await _dbContextFactory.CreateDbContextAsync(cancellationToken);
            dbContext.Attach(Execution);
            Execution.ExecutorProcessId = process.Id;
            Execution.ExecutionStatus = ExecutionStatus.Running;
            Execution.StartedOn = DateTimeOffset.Now;
            await dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            _healthService.AddError(Execution.ExecutionId,
                $"Error updating job execution status to Running: {ex.Message}");
            _logger.LogError(ex, "Error updating job execution status to Running");
            return;
        }

        // Run execution validations (circular dependencies etc.)
        foreach (var validator in _validators)
        {
            var result = await validator.ValidateAsync(
                execution: Execution,
                onValidationFailed: message => UpdateExecutionFailedAsync($"Execution failed validation. {message}"),
                cancellationToken);

            if (!result)
            {
                return;
            }
        }

        // Update execution parameter values for parameters that use expressions.
        try
        {
            await using var dbContext = await _dbContextFactory.CreateDbContextAsync(cancellationToken);
            foreach (var parameter in Execution.ExecutionParameters.Where(p => p.UseExpression))
            {
                dbContext.Attach(parameter);
                parameter.ParameterValue = new(await parameter.EvaluateAsync());
            }
            await dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "{executionId} Error evaluating execution parameters and saving evaluation results", Execution.ExecutionId);
            await UpdateExecutionFailedAsync("Error evaluating execution parameters and saving evaluation results");
            return;
        }

        try
        {
            // Don't pass cancellation token to notification task to not needlessly send notifications
            // if service is being shut down. Instead, pass new token which can be canceled if timeout is not reached.
            using var notificationCts = new CancellationTokenSource();
            var notificationTask = Execution.OvertimeNotificationLimitMinutes > 0
                ? Task.Delay(TimeSpan.FromMinutes(Execution.OvertimeNotificationLimitMinutes), notificationCts.Token)
                : Task.Delay(-1, notificationCts.Token); // infinite timeout

            var orchestrationTask = _jobOrchestrator.RunAsync(context, cancellationToken);

            await Task.WhenAny(notificationTask, orchestrationTask);

            // If the notification task completed first, send long-running notification.
            if (notificationTask.IsCompleted)
            {
                await _notificationService.SendLongRunningExecutionNotificationAsync(Execution);
            }
            await notificationCts.CancelAsync();
            await orchestrationTask; // Wait for orchestration to finish.
        }
        catch (Exception ex)
        {
            _healthService.AddError(Execution.ExecutionId,
                $"Error caught in job executor: {ex.Message}");
            _logger.LogError(ex, "Error during job execution");
        }

        // Update job execution status.
        try
        {
            await using var dbContext = await _dbContextFactory.CreateDbContextAsync(CancellationToken.None);
            dbContext.Attach(Execution);
            Execution.ExecutionStatus = Execution.GetCalculatedStatus();
            Execution.EndedOn = DateTimeOffset.Now;
            await dbContext.SaveChangesAsync(CancellationToken.None);
        }
        catch (Exception ex)
        {
            _healthService.AddError(Execution.ExecutionId,
                $"Error updating job execution status to Running: {ex.Message}");
            _logger.LogError(ex, "Error updating execution status");
        }

        // In case of cancellation (service shutdown), do not send notifications.
        cancellationToken.ThrowIfCancellationRequested();

        await _notificationService.SendCompletionNotificationAsync(Execution);
    }

    public void Cancel(string username) => _jobOrchestrator.CancelExecution(username);

    public void Cancel(string username, Guid stepId) => _jobOrchestrator.CancelExecution(username, stepId);

    private async Task UpdateExecutionFailedAsync(string errorMessage)
    {
        await using var context = await _dbContextFactory.CreateDbContextAsync();
        
        foreach (var attempt in Execution.StepExecutions.SelectMany(s => s.StepExecutionAttempts))
        {
            context.Attach(attempt);
            attempt.StartedOn = DateTimeOffset.Now;
            attempt.EndedOn = DateTimeOffset.Now;
            attempt.ExecutionStatus = StepExecutionStatus.Failed;
            attempt.AddError(errorMessage);
        }

        context.Attach(Execution);
        Execution.StartedOn = DateTimeOffset.Now;
        Execution.EndedOn = DateTimeOffset.Now;
        Execution.ExecutionStatus = ExecutionStatus.Failed;
        // Do not cancel saving failed status.
        await context.SaveChangesAsync();
    }
}