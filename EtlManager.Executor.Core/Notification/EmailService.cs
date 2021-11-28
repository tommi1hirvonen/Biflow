using EtlManager.DataAccess;
using EtlManager.DataAccess.Models;
using EtlManager.Utilities;
using Microsoft.EntityFrameworkCore;
using Serilog;
using System.Net.Mail;

namespace EtlManager.Executor.Core.Notification;

public class EmailService : INotificationService
{
    private readonly IEmailConfiguration _emailConfiguration;
    private readonly IDbContextFactory<EtlManagerContext> _dbContextFactory;


    public EmailService(IEmailConfiguration emailConfiguration, IDbContextFactory<EtlManagerContext> dbContextFactory)
    {
        _emailConfiguration = emailConfiguration;
        _dbContextFactory = dbContextFactory;
    }

    public async Task SendCompletionNotification(Execution execution, bool notify, SubscriptionType? notifyMe)
    {
        if (!notify && notifyMe is null) return;

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

        if (notify)
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
                Log.Error(ex, "{ExecutionId} Error getting recipients for notification", execution.ExecutionId);
                return;
            }
        }
        
        if (notifyMe is not null && subscriptionTypeFilter.Any(f => f == notifyMe))
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
                Log.Error(ex, "{ExecutionId} Error getting launcher user name for notification", execution.ExecutionId);
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
            .Where(e => e.ExecutionStatus != StepExecutionStatus.Succeeded)
            .Select(e => $@"
<tr>
    <td>{e.StepExecution.StepName}</td>
    <td>{e.StepType}</td>
    <td>{e.StartDateTime}</td>
    <td>{e.EndDateTime}</td>
    <td>{e.GetDurationInReadableFormat()}</td>
    <td>{e.ExecutionStatus}</td>
    <td>{e.ErrorMessage}</td>
</tr>
");

            messageBody = $@"
<html>
    <head>
        <style>
            body {{
                font-family: system-ui;
            }}
            table {{
                border-collapse: collapse;
            }}
            th {{
                padding: 8px;
	            background-color: #ccc;
            }}
            td {{
                padding: 8px;
            }}
            tr:nth-child(even) {{
                background-color: #f5f5f5;
            }}
        </style>
    </head>
    <body>
        <h3>{execution.JobName}</h3>
        <hr />
        <table>
            <tbody>
                <tr>
                    <td><strong>Status:</strong></td>
                    <td><span style=""color:{statusColor};""><strong>{execution.ExecutionStatus}</strong></span></td>
                </tr>
                <tr>
                    <td>Start time:</td>
                    <td>{execution.StartDateTime}</td>
                </tr>
                <tr>
                    <td>End time:</td>
                    <td>{execution.EndDateTime}</td>
                </tr>
                <tr>
                    <td>Duration:</td>
                    <td>{execution.GetDurationInReadableFormat()}</td >
                </tr>
            </tbody>
        </table>
        <h4>Failed steps</h4>
        <table border=""1"">
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
                {string.Join("\n", failedSteps)}
            </tbody>
        </table>
    </body>
</html>
";
        }
        catch (Exception ex)
        {
            Log.Error(ex, "{ExecutionId} Error building notification message body", execution.ExecutionId);
            // Do not return. The notification can be sent even without a body.
        }
        

        SmtpClient client;
        try
        {
            client = _emailConfiguration.Client;
        }
        catch (Exception ex)
        {
            Log.Error(ex, "{ExecutionId} Error building notification email SMTP client. Check appsettings.json.", execution.ExecutionId);
            return;
        }

        MailMessage mailMessage;
        try
        {
            mailMessage = new MailMessage
            {
                From = new MailAddress(_emailConfiguration.FromAddress),
                Subject = $"{execution.JobName} completed with status {execution.ExecutionStatus} – ETL Manager notification",
                IsBodyHtml = true,
                Body = messageBody
            };
        }
        catch (Exception ex)
        {
            Log.Error(ex, "{ExecutionId} Error building notification email message object. Check appsettings.json.", execution.ExecutionId);
            return;
        }

        recipients.ForEach(recipient => mailMessage.To.Add(recipient));

        try
        {
            await client.SendMailAsync(mailMessage);
            Log.Information("{ExecutionId} Notification email sent to: " + string.Join(", ", recipients), execution.ExecutionId);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "{ExecutionId} Error sending notification email", execution.ExecutionId);
        }
    }

    public async Task SendLongRunningExecutionNotification(Execution execution, bool notify, bool notifyMeOvertime)
    {
        if (!notify && !notifyMeOvertime) return;
        
        List<string> recipients = new();
        using var context = _dbContextFactory.CreateDbContext();
        if (notify)
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
                Log.Error(ex, "{ExecutionId} Error getting recipients for long running execution notification", execution.ExecutionId);
                return;
            }
        }
        
        if (notifyMeOvertime)
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
                Log.Error(ex, "{ExecutionId} Error getting launcher user name for long running execution notification", execution.ExecutionId);
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
            Log.Error(ex, "{ExecutionId} Error building long running execution notification email SMTP client. Check appsettings.json.", execution.ExecutionId);
            return;
        }

        MailMessage mailMessage;
        try
        {
            mailMessage = new MailMessage
            {
                From = new MailAddress(_emailConfiguration.FromAddress),
                Subject = $"\"{execution.JobName}\" execution is running long – ETL Manager notification",
                IsBodyHtml = true,
                Body = $"Execution of job \"{execution.JobName}\" started at {execution.StartDateTime?.LocalDateTime}"
                + $" has exceeded the overtime limit of {execution.OvertimeNotificationLimitMinutes} minutes set for this job."
            };
        }
        catch (Exception ex)
        {
            Log.Error(ex, "{ExecutionId} Error building notification email message object. Check appsettings.json.", execution.ExecutionId);
            return;
        }

        recipients.ForEach(recipient => mailMessage.To.Add(recipient));

        try
        {
            await client.SendMailAsync(mailMessage);
            Log.Information("{ExecutionId} Long running execution notification email sent to: " + string.Join(", ", recipients), execution.ExecutionId);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "{ExecutionId} Error sending notification email", execution.ExecutionId);
        }
    }

}
