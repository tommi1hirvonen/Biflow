using Dapper;
using EtlManagerDataAccess;
using EtlManagerDataAccess.Models;
using EtlManagerExecutor.Notification;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Serilog;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Net;
using System.Net.Mail;
using System.Text;

namespace EtlManagerExecutor
{
    public static class EmailHelper
    {
        public static void SendNotification(IConfiguration configuration, IDbContextFactory<EtlManagerContext> dbContextFactory, Guid executionId)
        {
            using var sqlConnection = new SqlConnection(configuration.GetConnectionString("EtlManagerContext"));
            using var context = dbContextFactory.CreateDbContext();
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
                "FAILED" or "STOPPED" or "SUSPENDED" or "NOT STARTED" or "RUNNING" =>
                new SubscriptionType[] { SubscriptionType.OnFailure, SubscriptionType.OnCompletion },
                "SUCCEEDED" or "WARNING" =>
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
                emailSettings = EmailSettings.FromConfiguration(configuration);
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
