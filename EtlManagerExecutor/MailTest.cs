using Microsoft.Extensions.Configuration;
using Serilog;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Mail;
using System.Text;

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
                Log.Error(ex, "Error getting email settings from appsettings.json");
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
                Log.Error(ex, "Error building email SMTP client. Check appsettings.json.");
                return;
            }

            MailMessage mailMessage;
            try
            {
                mailMessage = new MailMessage
                {
                    From = new MailAddress(fromAddress),
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
