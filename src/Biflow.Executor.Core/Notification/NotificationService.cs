using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Biflow.Executor.Core.Notification;

internal class NotificationService(
    ILogger<NotificationService> logger,
    [FromKeyedServices(ExecutorServiceKeys.NotificationHealthService)] HealthService healthService,
    ISubscribersResolver subscribersResolver,
    IMessageDispatcher? messageDispatcher = null) : INotificationService
{
    private readonly ILogger<NotificationService> _logger = logger;
    private readonly HealthService _healthService = healthService;
    private readonly IMessageDispatcher? _messageDispatcher = messageDispatcher;
    private readonly ISubscribersResolver _subscribersResolver = subscribersResolver;

    public async Task SendCompletionNotificationAsync(Execution execution)
    {
        if (execution is { Notify: false, NotifyCaller: null })
        {
            return;
        }

        IEnumerable<string> recipients;
        try
        {
            recipients = await _subscribersResolver.ResolveSubscriberEmailsAsync(execution);
        }
        catch (Exception ex)
        {
            _healthService.AddError(execution.ExecutionId, $"Error getting recipients for notification: ${ex.Message}");
            _logger.LogError(ex, "{ExecutionId} Error getting recipients for notification", execution.ExecutionId);
            return;
        }

        if (!recipients.Any())
        { 
            return;
        }

        if (_messageDispatcher is null)
        {
            _healthService.AddError(execution.ExecutionId,
                "Error sending notification mail: no message dispatcher was configured in the application settings.");
            _logger.LogError(
                "{ExecutionId} Error sending notification email. " +
                "No message dispatcher was configured in the application settings.",
                execution.ExecutionId);
            return;
        }

        var messageBody = string.Empty;
        try
        {
            var statusColor = execution.ExecutionStatus switch
            {
                ExecutionStatus.Succeeded => "#00b400", // green
                ExecutionStatus.Failed => "#dc0000", // red
                _ => "#ffc800" // orange
            };

            var failedSteps = execution
            .StepExecutions
            .SelectMany(e => e.StepExecutionAttempts)
            .Where(e => e.ExecutionStatus != StepExecutionStatus.Succeeded
                    && e.ExecutionStatus != StepExecutionStatus.Warning
                    && e.ExecutionStatus != StepExecutionStatus.AwaitingRetry
                    && e.ExecutionStatus != StepExecutionStatus.Retry
                    && e.ExecutionStatus != StepExecutionStatus.Skipped)
            .Select(e => $"""
            <tr>
                <td>{e.StepExecution.StepName}</td>
                <td>{e.StepType}</td>
                <td>{e.StartedOn}</td>
                <td>{e.EndedOn}</td>
                <td>{e.GetDurationInReadableFormat()}</td>
                <td>{e.ExecutionStatus}</td>
                <td>{string.Join("\n\n", e.ErrorMessages.Select(m => m.Message))}</td>
            </tr>
            """);

            messageBody = $$"""
            <html>
                <head>
                    <style>
                        body {
                            font-family: system-ui;
                        }
                        table {
                            border-collapse: collapse;
                        }
                        th {
                            padding: 8px;
                            background-color: #ccc;
                        }
                        td {
                            padding: 8px;
                        }
                        tr:nth-child(even) {
                            background-color: #f5f5f5;
                        }
                    </style>
                </head>
                <body>
                    <h3>{{execution.JobName}}</h3>
                    <hr />
                    <table>
                        <tbody>
                            <tr>
                                <td><strong>Status:</strong></td>
                                <td><span style="color:{{statusColor}};"><strong>{{execution.ExecutionStatus}}</strong></span></td>
                            </tr>
                            <tr>
                                <td>Start time:</td>
                                <td>{{execution.StartedOn}}</td>
                            </tr>
                            <tr>
                                <td>End time:</td>
                                <td>{{execution.EndedOn}}</td>
                            </tr>
                            <tr>
                                <td>Duration:</td>
                                <td>{{execution.GetDurationInReadableFormat()}}</td>
                            </tr>
                            <tr>
                                <td>Created by:</td>
                                <td>{{execution.ScheduleName?.NullIfEmpty() ?? execution.CreatedBy}}</td>
                            </tr>
                            <tr>
                                <td>Number of steps:</td>
                                <td>{{execution.StepExecutions.Count}}</td>
                            </tr>
                        </tbody>
                    </table>
                    <h4>Failed steps</h4>
                    <table border="1" style="font-size: small;">
                        <thead>
                            <tr>
                                <th>Step name</th>
                                <th>Step type</th>
                                <th>Start time</th>
                                <th>End time</th>
                                <th>Duration</th>
                                <th>Status</th>
                                <th>Error message</th>
                            </tr>
                        </thead>
                        <tbody>
                            {{string.Join("\n", failedSteps)}}
                        </tbody>
                    </table>
                </body>
            </html>
            """;
        }
        catch (Exception ex)
        {
            _healthService.AddError(execution.ExecutionId, $"Error building notification message body: ${ex.Message}");
            _logger.LogError(ex, "{ExecutionId} Error building notification message body", execution.ExecutionId);
            // Do not return. The notification can be sent even without a body.
        }

        try
        {
            var subject = $"{execution.JobName} completed with status {execution.ExecutionStatus} – Biflow notification";
            await _messageDispatcher.SendMessageAsync(recipients, subject, messageBody, true);
            _logger.LogInformation("{ExecutionId} Notification email sent to: {recipients}",
                execution.ExecutionId, string.Join(", ", recipients));
        }
        catch (Exception ex)
        {
            _healthService.AddError(execution.ExecutionId, $"Error sending notification mail: ${ex.Message}");
            _logger.LogError(ex, "{ExecutionId} Error sending notification email.", execution.ExecutionId);
        }
    }

    public async Task SendLongRunningExecutionNotificationAsync(Execution execution)
    {
        if (execution is { Notify: false, NotifyCallerOvertime: false })
        {
            return;
        }

        IEnumerable<string> recipients;
        try
        {
            recipients = await _subscribersResolver.ResolveLongRunningSubscriberEmailsAsync(execution);
        }
        catch (Exception ex)
        {
            _healthService.AddError(execution.ExecutionId,
                $"Error getting recipients for long running execution notification: ${ex.Message}");
            _logger.LogError(ex, "{ExecutionId} Error getting recipients for long running execution notification",
                execution.ExecutionId);
            return;
        }

        if (!recipients.Any())
        {
            return;
        }
        
        if (_messageDispatcher is null)
        {
            _healthService.AddError(execution.ExecutionId,
                "Error sending long-running execution notification mail: " +
                "no message dispatcher was configured in the application settings.");
            _logger.LogError(
                "{ExecutionId} Error sending long-running execution notification email. " +
                "No message dispatcher was configured in the application settings.",
                execution.ExecutionId);
            return;
        }

        try
        {
            var subject = $"\"{execution.JobName}\" execution is running long – Biflow notification";
            var messageBody = $"Execution of job \"{execution.JobName}\" started at {execution.StartedOn?.LocalDateTime}"
                + $" has exceeded the overtime limit of {execution.OvertimeNotificationLimitMinutes} minutes set for this job.";
            await _messageDispatcher.SendMessageAsync(recipients, subject, messageBody, false);
            _logger.LogInformation("{ExecutionId} Long-running execution notification email sent to: {recipients}",
                execution.ExecutionId, string.Join(", ", recipients));
        }
        catch (Exception ex)
        {
            _healthService.AddError(execution.ExecutionId,
                $"Error sending long-running execution notification: ${ex.Message}");
            _logger.LogError(ex, "{ExecutionId} Error sending notification email.", execution.ExecutionId);
        }
    }
}
