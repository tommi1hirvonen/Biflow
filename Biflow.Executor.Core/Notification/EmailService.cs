using Biflow.Core;
using Biflow.DataAccess.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Net.Mail;
using System.Runtime.CompilerServices;

[assembly: InternalsVisibleTo("Biflow.Executor.ConsoleApp.Test")]
namespace Biflow.Executor.Core.Notification;

internal class EmailService : INotificationService
{
    private readonly ILogger<EmailService> _logger;
    private readonly IEmailConfiguration _emailConfiguration;
    private readonly IDbContextFactory<ExecutorDbContext> _dbContextFactory;


    public EmailService(ILogger<EmailService> logger, IEmailConfiguration emailConfiguration, IDbContextFactory<ExecutorDbContext> dbContextFactory)
    {
        _logger = logger;
        _emailConfiguration = emailConfiguration;
        _dbContextFactory = dbContextFactory;
    }

    public async Task SendCompletionNotification(Execution execution)
    {
        if (!execution.Notify && execution.NotifyCaller is null) return;

        var subscriptionTypeFilter = execution.ExecutionStatus switch
        {
            ExecutionStatus.Failed or ExecutionStatus.Stopped or ExecutionStatus.Suspended or ExecutionStatus.NotStarted or ExecutionStatus.Running =>
            new SubscriptionType[] { SubscriptionType.OnFailure, SubscriptionType.OnCompletion },
            ExecutionStatus.Succeeded or ExecutionStatus.Warning =>
            new SubscriptionType[] { SubscriptionType.OnSuccess, SubscriptionType.OnCompletion },
            _ =>
            new SubscriptionType[] { SubscriptionType.OnCompletion }
        };

        using var context = _dbContextFactory.CreateDbContext();

        List<string> recipients = new();

        if (execution.Notify)
        {
            try
            {
                var subscriptions = await context.Subscriptions
                    .AsNoTrackingWithIdentityResolution()
                    .Include(s => s.User)
                    .Where(s => s.User.Email != null && s.JobId == execution.JobId)
                    .ToListAsync();
                var subscribers = subscriptions
                    .Where(s => subscriptionTypeFilter.Any(f => f == s.SubscriptionType))
                    .Select(s => s.User.Email ?? "");
                recipients.AddRange(subscribers);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "{ExecutionId} Error getting recipients for notification", execution.ExecutionId);
                return;
            }
        }
        
        if (execution.NotifyCaller is not null && subscriptionTypeFilter.Any(f => f == execution.NotifyCaller))
        {
            try
            {
                var user = await context.Users.FirstOrDefaultAsync(u => u.Username == execution.CreatedBy);
                if (user?.Email is not null && !recipients.Contains(user.Email))
                {
                    recipients.Add(user.Email);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "{ExecutionId} Error getting launcher user name for notification", execution.ExecutionId);
            }
        }

        if (!recipients.Any())
            return;

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
        

        SmtpClient client;
        try
        {
            client = _emailConfiguration.Client;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "{ExecutionId} Error building notification email SMTP client. Check appsettings.json.", execution.ExecutionId);
            return;
        }

        MailMessage mailMessage;
        try
        {
            ArgumentException.ThrowIfNullOrEmpty(_emailConfiguration.FromAddress);
            mailMessage = new MailMessage
            {
                From = new MailAddress(_emailConfiguration.FromAddress),
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

        recipients.ForEach(mailMessage.Bcc.Add);

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
        if (!execution.Notify && !execution.NotifyCallerOvertime) return;
        
        List<string> recipients = new();
        using var context = _dbContextFactory.CreateDbContext();
        if (execution.Notify)
        {
            try
            {
                var subscriptions = await context.Subscriptions
                    .AsNoTrackingWithIdentityResolution()
                    .Include(s => s.User)
                    .Where(s => s.User.Email != null && s.JobId == execution.JobId)
                    .ToListAsync();
                var subscribers = subscriptions
                    .Where(s => s.NotifyOnOvertime)
                    .Select(s => s.User.Email ?? "");
                recipients.AddRange(subscribers);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "{ExecutionId} Error getting recipients for long running execution notification", execution.ExecutionId);
                return;
            }
        }
        
        if (execution.NotifyCallerOvertime)
        {
            try
            {
                var user = await context.Users.FirstOrDefaultAsync(u => u.Username == execution.CreatedBy);
                if (user?.Email is not null && !recipients.Contains(user.Email))
                {
                    recipients.Add(user.Email);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "{ExecutionId} Error getting launcher user name for long running execution notification", execution.ExecutionId);
            }
        }

        if (!recipients.Any())
            return;

        SmtpClient client;
        try
        {
            client = _emailConfiguration.Client;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "{ExecutionId} Error building long running execution notification email SMTP client. Check appsettings.json.", execution.ExecutionId);
            return;
        }

        MailMessage mailMessage;
        try
        {
            ArgumentException.ThrowIfNullOrEmpty(_emailConfiguration.FromAddress);
            mailMessage = new MailMessage
            {
                From = new MailAddress(_emailConfiguration.FromAddress),
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

        recipients.ForEach(recipient => mailMessage.Bcc.Add(recipient));

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
        ArgumentException.ThrowIfNullOrEmpty(_emailConfiguration.FromAddress);
        var client = _emailConfiguration.Client;
        var mailMessage = new MailMessage
        {
            From = new MailAddress(_emailConfiguration.FromAddress),
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
