using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Mail;
using System.Text;

namespace EtlManagerExecutor.Notification
{
    public class EmailSettings
    {
        public string SmtpServer { get; set; }
        public bool EnableSsl { get; set; }
        public int Port { get; set; }
        public string FromAddress { get; set; }
        public bool AnonymousAuthentication { get; set; }
        public string Username { get; set; }
        public string Password { get; set; }

        public SmtpClient GetSmtpClient()
        {
            if (AnonymousAuthentication)
            {
                return new SmtpClient(SmtpServer);
            }
            else
            {
                return new SmtpClient(SmtpServer)
                {
                    UseDefaultCredentials = false,
                    Credentials = new NetworkCredential(Username, Password),
                    EnableSsl = EnableSsl,
                    Port = Port
                };
            }
        }

        public static EmailSettings FromConfiguration(IConfiguration configuration)
        {
            IConfigurationSection emailSettings = configuration.GetSection("EmailSettings");
            return new EmailSettings()
            {
                SmtpServer = emailSettings.GetValue<string>("SmtpServer"),
                EnableSsl = emailSettings.GetValue<bool>("EnableSsl"),
                Port = emailSettings.GetValue<int>("Port"),
                FromAddress = emailSettings.GetValue<string>("FromAddress"),
                AnonymousAuthentication = emailSettings.GetValue<bool>("AnonymousAuthentication"),
                Username = emailSettings.GetValue<string>("Username"),
                Password = emailSettings.GetValue<string>("Password")
            };
        }
    }
}
