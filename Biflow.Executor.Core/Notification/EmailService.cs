using Biflow.Core;
using Biflow.DataAccess.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Net.Mail;

namespace Biflow.Executor.Core.Notification;

internal class EmailService : INotificationService
{
    private readonly ILogger<EmailService> _logger;
    private readonly IOptionsMonitor<EmailOptions> _options;
    private readonly ISubscribersResolver _subscribersResolver;

    public EmailService(
        ILogger<EmailService> logger,
        IOptionsMonitor<EmailOptions> options,
        ISubscribersResolver subscribersResolver)
    {
        _logger = logger;
        _options = options;
        _subscribersResolver = subscribersResolver;
    }

    public async Task SendCompletionNotification(Execution execution)
    {
        if (!execution.Notify && execution.NotifyCaller is null)
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
            _logger.LogError(ex, "{ExecutionId} Error getting recipients for notification", execution.ExecutionId);
            return;
        }

        if (!recipients.Any())
        { 
            return;
        }

        string messageBody = string.Empty;

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
                    && e.ExecutionStatus != StepExecutionStatus.AwaitingRetry)
            .Select(e => $"""
            <tr>
                <td>{e.StepExecution.StepName}</td>
                <td>{e.StepType}</td>
                <td>{e.StartDateTime}</td>
                <td>{e.EndDateTime}</td>
                <td>{e.GetDurationInReadableFormat()}</td>
                <td>{e.ExecutionStatus}</td>
                <td>{e.ErrorMessage}</td>
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
                                <td>{{execution.StartDateTime}}</td>
                            </tr>
                            <tr>
                                <td>End time:</td>
                                <td>{{execution.EndDateTime}}</td>
                            </tr>
                            <tr>
                                <td>Duration:</td>
                                <td>{{execution.GetDurationInReadableFormat()}}</td >
                            </tr>
                        </tbody>
                    </table>
                    <h4>Failed steps</h4>
                    <table border="1">
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
            _logger.LogError(ex, "{ExecutionId} Error building notification message body", execution.ExecutionId);
            // Do not return. The notification can be sent even without a body.
        }

        var options = _options.CurrentValue;
        SmtpClient client;
        try
        {
            client = options.Client;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "{ExecutionId} Error building notification email SMTP client. Check appsettings.json.", execution.ExecutionId);
            return;
        }

        MailMessage mailMessage;
        try
        {
            ArgumentException.ThrowIfNullOrEmpty(options.FromAddress);
            mailMessage = new MailMessage
            {
                From = new MailAddress(options.FromAddress),
                Subject = $"{execution.JobName} completed with status {execution.ExecutionStatus} – Biflow notification",
                IsBodyHtml = true,
                Body = messageBody
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "{ExecutionId} Error building notification email message object. Check appsettings.json.", execution.ExecutionId);
            return;
        }

        foreach (var recipient in recipients)
        {
            mailMessage.Bcc.Add(recipient);
        }

        try
        {
            await client.SendMailAsync(mailMessage);
            _logger.LogInformation("{ExecutionId} Notification email sent to: {recipients}", execution.ExecutionId, string.Join(", ", recipients));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "{ExecutionId} Error sending notification email", execution.ExecutionId);
        }
    }

    public async Task SendLongRunningExecutionNotification(Execution execution)
    {
        IEnumerable<string> recipients;
        try
        {
            recipients = await _subscribersResolver.ResolveLongRunningSubscriberEmailsAsync(execution);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "{ExecutionId} Error getting recipients for long running execution notification", execution.ExecutionId);
            return;
        }

        if (!recipients.Any())
        {
            return;
        }

        var options = _options.CurrentValue;
        SmtpClient client;
        try
        {
            client = options.Client;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "{ExecutionId} Error building long running execution notification email SMTP client. Check appsettings.json.", execution.ExecutionId);
            return;
        }

        MailMessage mailMessage;
        try
        {
            ArgumentException.ThrowIfNullOrEmpty(options.FromAddress);
            mailMessage = new MailMessage
            {
                From = new MailAddress(options.FromAddress),
                Subject = $"\"{execution.JobName}\" execution is running long – Biflow notification",
                IsBodyHtml = true,
                Body = $"Execution of job \"{execution.JobName}\" started at {execution.StartDateTime?.LocalDateTime}"
                + $" has exceeded the overtime limit of {execution.OvertimeNotificationLimitMinutes} minutes set for this job."
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "{ExecutionId} Error building notification email message object. Check appsettings.json.", execution.ExecutionId);
            return;
        }

        foreach (var recipient in recipients)
        {
            mailMessage.Bcc.Add(recipient);
        }

        try
        {
            await client.SendMailAsync(mailMessage);
            _logger.LogInformation("{ExecutionId} Long running execution notification email sent to: {recipients}", execution.ExecutionId, string.Join(", ", recipients));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "{ExecutionId} Error sending notification email", execution.ExecutionId);
        }
    }

    public async Task SendNotification(IEnumerable<string> recipients, string subject, string body, CancellationToken cancellationToken)
    {
        var options = _options.CurrentValue;
        ArgumentException.ThrowIfNullOrEmpty(options.FromAddress);
        var client = options.Client;
        var mailMessage = new MailMessage
        {
            From = new MailAddress(options.FromAddress),
            Subject = subject,
            Body = body
        };
        foreach (var recipient in recipients)
        {
            mailMessage.Bcc.Add(recipient);
        }
        await client.SendMailAsync(mailMessage, cancellationToken);
    }
}
