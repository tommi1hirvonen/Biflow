using EtlManagerExecutor.Notification;
using Microsoft.Extensions.Configuration;
using Serilog;
using System;
using System.Net;
using System.Net.Mail;

namespace EtlManagerExecutor
{
    class MailTest : IMailTest
    {
        private readonly IConfiguration configuration;
        public MailTest(IConfiguration configuration)
        {
            this.configuration = configuration;
        }

        public void Run(string toAddress)
        {
            EmailSettings emailSettings;
            try
            {
                emailSettings = EmailSettings.FromConfiguration(configuration);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error getting email settings from appsettings.json");
                return;
            }

            SmtpClient client;
            try
            {
                client = emailSettings.GetSmtpClient();
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error building email SMTP client. Check appsettings.json.");
                return;
            }

            MailMessage mailMessage;
            try
            {
                mailMessage = new MailMessage
                {
                    From = new MailAddress(emailSettings.FromAddress),
                    Subject = "ETL Manager Test Mail",
                    IsBodyHtml = true,
                    Body = "This is a test email sent from ETL Manager."
                };
                mailMessage.To.Add(toAddress);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error building message object. Check appsettings.json.");
                return;
            }

            client.Send(mailMessage);
        }
    }
}
