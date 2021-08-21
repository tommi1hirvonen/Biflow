using System;
using System.Net.Mail;
using System.Threading.Tasks;

namespace EtlManagerExecutor
{
    class EmailTest : IEmailTest
    {
        private readonly IEmailConfiguration _emailConfiguration;
        public EmailTest(IEmailConfiguration emailConfiguration)
        {
            _emailConfiguration = emailConfiguration;
        }

        public async Task RunAsync(string toAddress)
        {
            SmtpClient client;
            try
            {
                client = _emailConfiguration.Client;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error building email SMTP client. Check appsettings.json.\n{ex.Message}");
                return;
            }

            MailMessage mailMessage;
            try
            {
                mailMessage = new MailMessage
                {
                    From = new MailAddress(_emailConfiguration.FromAddress),
                    Subject = "ETL Manager Test Mail",
                    IsBodyHtml = true,
                    Body = "This is a test email sent from ETL Manager."
                };
                mailMessage.To.Add(toAddress);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error building message object. Check appsettings.json.\n{ex.Message}");
                return;
            }
            
            await client.SendMailAsync(mailMessage);
        }
    }
}
