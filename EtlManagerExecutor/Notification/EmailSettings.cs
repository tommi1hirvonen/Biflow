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
        public string SmtpServer { get; init; }
        public bool EnableSsl { get; init; }
        public int Port { get; init; }
        public string FromAddress { get; init; }
        public bool AnonymousAuthentication { get; init; }
        public string Username { get; init; }
        public string Password { get; init; }

        private EmailSettings(
            string smtpServer,
            bool enableSsl,
            int port,
            string fromAddress,
            bool anonymousAuthentication,
            string username,
            string password
            )
        {
            SmtpServer = smtpServer;
            EnableSsl = enableSsl;
            Port = port;
            FromAddress = fromAddress;
            AnonymousAuthentication = anonymousAuthentication;
            Username = username;
            Password = password;
        }

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
            return new EmailSettings(
                smtpServer: emailSettings.GetValue<string>("SmtpServer"),
                enableSsl: emailSettings.GetValue<bool>("EnableSsl"),
                port: emailSettings.GetValue<int>("Port"),
                fromAddress: emailSettings.GetValue<string>("FromAddress"),
                anonymousAuthentication: emailSettings.GetValue<bool>("AnonymousAuthentication"),
                username: emailSettings.GetValue<string>("Username"),
                password: emailSettings.GetValue<string>("Password")
            );
        }
    }
}
