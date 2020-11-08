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
            using SqlConnection sqlConnection = new SqlConnection(configuration.GetValue<string>("EtlManagerConnectionString"));
            sqlConnection.Open();

            SqlCommand jobInfoCmd = new SqlCommand(
                @"SELECT JobId, JobName, ExecutionStatus
                FROM etlmanager.vExecutionJob
                WHERE ExecutionId = @ExecutionId"
                , sqlConnection);
            jobInfoCmd.Parameters.AddWithValue("ExecutionId", executionId);

            string jobId = string.Empty;
            string jobName = string.Empty;
            string jobStatus = string.Empty;

            try
            {
                using var reader = jobInfoCmd.ExecuteReader();
                reader.Read();
                jobId = reader["JobId"].ToString();
                jobName = reader["JobName"].ToString();
                jobStatus = reader["ExecutionStatus"].ToString();
            }
            catch (Exception ex)
            {
                Log.Error(ex, "{executionId} Error getting execution status for notification evaluation", executionId);
                return;
            }
            

            if (jobStatus != "FAILED")
            {
                return;
            }

            SqlCommand recipientsCmd = new SqlCommand(
                @"SELECT DISTINCT B.[Email]
                FROM [etlmanager].[Subscription] AS A
                    INNER JOIN [etlmanager].[User] AS B ON A.[Username] = B.[Username]
                WHERE A.[JobId] = @JobId AND B.[Email] IS NOT NULL"
                , sqlConnection);
            recipientsCmd.Parameters.AddWithValue("@JobId", jobId);

            List<string> recipients = new List<string>();

            try
            {
                using var reader = recipientsCmd.ExecuteReader();
                while (reader.Read())
                {
                    recipients.Add(reader[0].ToString());
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


            SqlCommand messageBodyCmd = new SqlCommand("EXEC [etlmanager].[GetNotificationMessageBody] @ExecutionId", sqlConnection);
            messageBodyCmd.Parameters.AddWithValue("ExecutionId", executionId);

            var messageBody = string.Empty;
            try
            {
                messageBody = messageBodyCmd.ExecuteScalar().ToString();
            }
            catch (Exception ex)
            {
                Log.Error(ex, "{executionId} Error getting notification message body", executionId);
                // Do not return. The notification can be sent even without a body.
            }

            IConfigurationSection emailSettings;
            string smtpServer;
            bool enableSsl;
            int port;
            string fromAddress;
            string username;
            string password;
            try
            {
                emailSettings = configuration.GetSection("EmailSettings");
                smtpServer = emailSettings.GetValue<string>("SmtpServer");
                enableSsl = emailSettings.GetValue<bool>("EnableSsl");
                port = emailSettings.GetValue<int>("Port");
                fromAddress = emailSettings.GetValue<string>("FromAddress");
                username = emailSettings.GetValue<string>("Username");
                password = emailSettings.GetValue<string>("Password");
            }
            catch (Exception ex)
            {
                Log.Error(ex, "{executionId} Error getting email settings from appsettings.json", executionId);
                return;
            }


            SmtpClient client;
            try
            {
                client = new SmtpClient(smtpServer)
                {
                    UseDefaultCredentials = false,
                    Credentials = new NetworkCredential(username, password),
                    EnableSsl = enableSsl,
                    Port = port
                };
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
                    From = new MailAddress(fromAddress),
                    Subject = "ETL Manager Alert: " + jobName + " failed",
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
