using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Biflow.Executor.Core.Notification;

internal class NotificationService(
    ILogger<NotificationService> logger,
    INotificationMessageService notificationMessageService,
    [FromKeyedServices(ExecutorServiceKeys.NotificationHealthService)] HealthService healthService,
    ISubscribersResolver subscribersResolver,
    IMessageDispatcher? messageDispatcher = null) : INotificationService
{
    private readonly ILogger<NotificationService> _logger = logger;
    private readonly HealthService _healthService = healthService;
    private readonly IMessageDispatcher? _messageDispatcher = messageDispatcher;
    private readonly ISubscribersResolver _subscribersResolver = subscribersResolver;
    private readonly INotificationMessageService _notificationMessageService = notificationMessageService;

    public async Task<NotificationResponse> SendCompletionNotificationAsync(Execution execution)
    {
        if (execution is { Notify: false, NotifyCaller: null })
        {
            return NotificationResponse.Empty;
        }

        ICollection<string> recipients;
        try
        {
            recipients = await _subscribersResolver.ResolveSubscriberEmailsAsync(execution);
        }
        catch (Exception ex)
        {
            _healthService.AddError(execution.ExecutionId, $"Error getting recipients for notification: ${ex.Message}");
            _logger.LogError(ex, "{ExecutionId} Error getting recipients for notification", execution.ExecutionId);
            return NotificationResponse.Empty;
        }

        if (recipients.Count == 0)
        { 
            return NotificationResponse.Empty;
        }

        if (_messageDispatcher is null)
        {
            _healthService.AddError(execution.ExecutionId,
                "Error sending notification mail: no message dispatcher was configured in the application settings.");
            _logger.LogError(
                "{ExecutionId} Error sending notification email. " +
                "No message dispatcher was configured in the application settings.",
                execution.ExecutionId);
            return  NotificationResponse.Empty;
        }

        string messageBody;
        try
        {
            messageBody = await _notificationMessageService.CreateMessageBodyAsync(execution);
        }
        catch (Exception ex)
        {
            messageBody = string.Empty;
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
            return new NotificationResponse(recipients, subject, messageBody, true);
        }
        catch (Exception ex)
        {
            _healthService.AddError(execution.ExecutionId, $"Error sending notification mail: ${ex.Message}");
            _logger.LogError(ex, "{ExecutionId} Error sending notification email.", execution.ExecutionId);
            return  NotificationResponse.Empty;
        }
    }

    public async Task<NotificationResponse> SendLongRunningExecutionNotificationAsync(Execution execution)
    {
        if (execution is { Notify: false, NotifyCallerOvertime: false })
        {
            return NotificationResponse.Empty;
        }

        ICollection<string> recipients;
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
            return  NotificationResponse.Empty;
        }

        if (recipients.Count == 0)
        {
            return  NotificationResponse.Empty;
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
            return   NotificationResponse.Empty;
        }

        try
        {
            var subject = $"\"{execution.JobName}\" execution is running long – Biflow notification";
            var messageBody = $"Execution of job \"{execution.JobName}\" started at {execution.StartedOn?.LocalDateTime}"
                + $" has exceeded the overtime limit of {execution.OvertimeNotificationLimitMinutes} minutes set for this job.";
            await _messageDispatcher.SendMessageAsync(recipients, subject, messageBody, false);
            _logger.LogInformation("{ExecutionId} Long-running execution notification email sent to: {recipients}",
                execution.ExecutionId, string.Join(", ", recipients));
            return new NotificationResponse(recipients, subject, messageBody, false);
        }
        catch (Exception ex)
        {
            _healthService.AddError(execution.ExecutionId,
                $"Error sending long-running execution notification: ${ex.Message}");
            _logger.LogError(ex, "{ExecutionId} Error sending notification email.", execution.ExecutionId);
            return NotificationResponse.Empty;
        }
    }
}
