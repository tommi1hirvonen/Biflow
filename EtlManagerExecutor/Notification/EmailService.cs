using Dapper;
using EtlManagerDataAccess;
using EtlManagerDataAccess.Models;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Mail;
using System.Text;

namespace EtlManagerExecutor
{
    public class EmailService : INotificationService
    {
        private readonly IEmailConfiguration _emailConfiguration;
        private readonly IDbContextFactory<EtlManagerContext> _dbContextFactory;


        public EmailService(IEmailConfiguration emailConfiguration, IDbContextFactory<EtlManagerContext> dbContextFactory)
        {
            _emailConfiguration = emailConfiguration;
            _dbContextFactory = dbContextFactory;
        }

        public void SendCompletionNotification(Guid executionId)
        {
            using var context = _dbContextFactory.CreateDbContext();
            Execution execution;
            try
            {
                execution = context.Executions.Find(executionId);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "{executionId} Error getting execution status for notification evaluation", executionId);
                return;
            }

            var subscriptionTypeFilter = execution.ExecutionStatus switch
            {
                ExecutionStatus.Failed or ExecutionStatus.Stopped or ExecutionStatus.Suspended or ExecutionStatus.NotStarted or ExecutionStatus.Running =>
                new SubscriptionType[] { SubscriptionType.OnFailure, SubscriptionType.OnCompletion },
                ExecutionStatus.Succeeded or ExecutionStatus.Warning =>
                new SubscriptionType[] { SubscriptionType.OnSuccess, SubscriptionType.OnCompletion },
                _ =>
                new SubscriptionType[] { SubscriptionType.OnCompletion }
            };

            List<string> recipients;
            try
            {
                var subscriptions = context.Subscriptions
                    .Include(s => s.User)
                    .Where(s => s.User.Email != null && s.JobId == execution.JobId)
                    .ToList();
                recipients = subscriptions
                    .Where(s => subscriptionTypeFilter.Any(f => f == s.SubscriptionType))
                    .Select(s => s.User.Email ?? "")
                    .ToList();
            }
            catch (Exception ex)
            {
                Log.Error(ex, "{executionId} Error getting recipients for notification", executionId);
                return;
            }

            if (!recipients.Any())
                return;

            string messageBody = string.Empty;
            try
            {
                using var sqlConnection = new SqlConnection(context.Database.GetConnectionString());
                messageBody = sqlConnection.ExecuteScalar<string?>(
                    "EXEC [etlmanager].[GetNotificationMessageBody] @ExecutionId",
                    new { ExecutionId = executionId }) ?? string.Empty;
            }
            catch (Exception ex)
            {
                Log.Error(ex, "{executionId} Error getting notification message body", executionId);
                // Do not return. The notification can be sent even without a body.
            }

            SmtpClient client;
            try
            {
                client = _emailConfiguration.Client;
            }
            catch (Exception ex)
            {
                Log.Error(ex, "{executionId} Error building notification email SMTP client. Check appsettings.json.", executionId);
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
                Log.Error(ex, "{executionId} Error building notification email message object. Check appsettings.json.", executionId);
                return;
            }

            recipients.ForEach(recipient => mailMessage.To.Add(recipient));

            try
            {
                client.Send(mailMessage);
                Log.Information("{executionId} Notification email sent to: " + string.Join(", ", recipients), executionId);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "{executionId} Error sending notification email", executionId);
            }
        }

        public void SendLongRunningExecutionNotification(Execution execution)
        {
            List<string> recipients;
            try
            {
                using var context = _dbContextFactory.CreateDbContext();
                var subscriptions = context.Subscriptions
                    .Include(s => s.User)
                    .Where(s => s.User.Email != null && s.JobId == execution.JobId)
                    .ToList();
                recipients = subscriptions
                    .Where(s => s.NotifyOnOvertime)
                    .Select(s => s.User.Email ?? "")
                    .ToList();
            }
            catch (Exception ex)
            {
                Log.Error(ex, "{ExecutionId} Error getting recipients for long running execution notification", execution.ExecutionId);
                return;
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
                client.Send(mailMessage);
                Log.Information("{ExecutionId} Long running execution notification email sent to: " + string.Join(", ", recipients), execution.ExecutionId);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "{ExecutionId} Error sending notification email", execution.ExecutionId);
            }
        }

    }
}
