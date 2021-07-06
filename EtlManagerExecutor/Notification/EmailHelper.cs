using Dapper;
using EtlManagerExecutor.Notification;
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
        public static void SendNotification(IConfiguration configuration, string executionId)
        {
            using var sqlConnection = new SqlConnection(configuration.GetValue<string>("EtlManagerConnectionString"));

            Guid jobId;
            string jobName;
            string jobStatus;
            try
            {
                (jobId, jobName, jobStatus) = sqlConnection.QueryFirst<(Guid, string, string)>(
                    @"SELECT JobId, JobName, ExecutionStatus
                    FROM etlmanager.vExecutionJob
                    WHERE ExecutionId = @ExecutionId",
                    new { ExecutionId = executionId });
            }
            catch (Exception ex)
            {
                Log.Error(ex, "{executionId} Error getting execution status for notification evaluation", executionId);
                return;
            }

            var subscriptionTypeFilter = jobStatus switch
            {
                "FAILED" or "STOPPED" or "SUSPENDED" or "NOT STARTED" or "RUNNING" => new string[] { "FAILURE", "COMPLETION" },
                "SUCCEEDED" or "WARNING" => new string[] { "SUCCESS", "COMPLETION" },
                _ => new string[] { "COMPLETION" }
            };

            List<string> recipients;
            try
            {
                recipients = sqlConnection.Query<string>(
                    @"SELECT DISTINCT B.[Email]
                    FROM [etlmanager].[Subscription] AS A
                        INNER JOIN [etlmanager].[User] AS B ON A.[Username] = B.[Username]
                    WHERE A.[JobId] = @JobId AND B.[Email] IS NOT NULL AND A.[SubscriptionType] IN @SubscriptionTypeFilter",
                    new { JobId = jobId, SubscriptionTypeFilter = subscriptionTypeFilter }).ToList();
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
                    Subject = $"{jobName} completed with status {jobStatus} – ETL Manager notification",
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
