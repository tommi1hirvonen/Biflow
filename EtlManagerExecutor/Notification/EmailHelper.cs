using EtlManagerExecutor.Notification;
using Microsoft.Extensions.Configuration;
using Serilog;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
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
            sqlConnection.Open();

            string jobId = string.Empty;
            string jobName = string.Empty;
            string jobStatus = string.Empty;
            try
            {
                using var jobInfoCmd = new SqlCommand(
                    @"SELECT JobId, JobName, ExecutionStatus
                    FROM etlmanager.vExecutionJob
                    WHERE ExecutionId = @ExecutionId"
                    , sqlConnection);
                jobInfoCmd.Parameters.AddWithValue("ExecutionId", executionId);
                using var reader = jobInfoCmd.ExecuteReader();
                reader.Read();
                jobId = reader["JobId"].ToString()!;
                jobName = reader["JobName"].ToString()!;
                jobStatus = reader["ExecutionStatus"].ToString()!;
            }
            catch (Exception ex)
            {
                Log.Error(ex, "{executionId} Error getting execution status for notification evaluation", executionId);
                return;
            }

            string subscriptionTypeFilter = jobStatus switch
            {
                "FAILED" or "STOPPED" or "SUSPENDED" or "NOT STARTED" or "RUNNING" => "'FAILURE', 'COMPLETION'",
                "SUCCEEDED" or "WARNING" => "'SUCCESS', 'COMPLETION'",
                _ => "'COMPLETION'"
            };

            var recipients = new List<string>();
            try
            {
                using var recipientsCmd = new SqlCommand(
                    "SELECT DISTINCT B.[Email] \n" +
                    "FROM [etlmanager].[Subscription] AS A \n" +
                        "INNER JOIN [etlmanager].[User] AS B ON A.[Username] = B.[Username] \n" +
                    $"WHERE A.[JobId] = @JobId AND B.[Email] IS NOT NULL AND A.[SubscriptionType] IN ({subscriptionTypeFilter})"
                    , sqlConnection);
                recipientsCmd.Parameters.AddWithValue("@JobId", jobId);
                using var reader = recipientsCmd.ExecuteReader();
                while (reader.Read())
                {
                    var recipient = reader[0].ToString();
                    if (recipient is not null)
                        recipients.Add(recipient);
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "{executionId} Error getting recipients for notification", executionId);
            }

            if (recipients.Count == 0)
            {
                return;
            }

            string messageBody = string.Empty;
            try
            {
                using var messageBodyCmd = new SqlCommand("EXEC [etlmanager].[GetNotificationMessageBody] @ExecutionId", sqlConnection);
                messageBodyCmd.Parameters.AddWithValue("ExecutionId", executionId);
                messageBody = messageBodyCmd.ExecuteScalar().ToString() ?? string.Empty;
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
