using Dapper;
using EtlManagerDataAccess;
using EtlManagerDataAccess.Models;
using EtlManagerExecutor.Notification;
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
    public class EmailHelper : INotificationService
    {
        private readonly IConfiguration _configuration;
        private readonly IDbContextFactory<EtlManagerContext> _dbContextFactory;
        private readonly IExecutionConfiguration _executionConfiguration;


        public EmailHelper(IConfiguration configuration, IDbContextFactory<EtlManagerContext> dbContextFactory, IExecutionConfiguration executionConfiguration)
        {
            _configuration = configuration;
            _dbContextFactory = dbContextFactory;
            _executionConfiguration = executionConfiguration;
        }

        public void SendNotification(Guid executionId)
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
                using var sqlConnection = new SqlConnection(_executionConfiguration.ConnectionString);
                messageBody = sqlConnection.ExecuteScalar<string?>(
                    "EXEC [etlmanager].[GetNotificationMessageBody] @ExecutionId",
                    new { ExecutionId = executionId }) ?? string.Empty;
            }
            catch (Exception ex)
            {
                Log.Error(ex, "{executionId} Error getting notification message body", executionId);
                // Do not return. The notification can be sent even without a body.
            }

            EmailSettings emailSettings;
            try
            {
                emailSettings = EmailSettings.FromConfiguration(_configuration);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "{executionId} Error getting email settings from appsettings.json", executionId);
                return;
            }

            SmtpClient client;
            try
            {
                client = emailSettings.GetSmtpClient();
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
                    From = new MailAddress(emailSettings.FromAddress),
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

    }
}
