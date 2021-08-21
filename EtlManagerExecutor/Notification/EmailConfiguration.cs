using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Mail;
using System.Text;

namespace EtlManagerExecutor
{
    public class EmailConfiguration : IEmailConfiguration
    {
        private readonly IConfiguration _configuration;

        public EmailConfiguration(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        private IConfigurationSection MailSection => _configuration.GetSection("EmailSettings");
        private string SmtpServer => MailSection.GetValue<string>("SmtpServer");
        private bool EnableSsl => MailSection.GetValue<bool>("EnableSsl");
        private int Port => MailSection.GetValue<int>("Port");
        public string FromAddress => MailSection.GetValue<string>("FromAddress");
        private bool AnonymousAuthentication => MailSection.GetValue<bool>("AnonymousAuthentication");
        private string Username => MailSection.GetValue<string>("Username");
        private string Password => MailSection.GetValue<string>("Password");
        public SmtpClient Client
        {
            get
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
        }
    }
}
