using System.ComponentModel.DataAnnotations.Schema;

namespace Biflow.DataAccess.Models;

public class EmailStepExecution : ParameterizedStepExecution
{
    public EmailStepExecution(string stepName, string recipients, string subject, string body) : base(stepName, StepType.Email)
    {
        Recipients = recipients;
        Subject = subject;
        Body = body;
    }

    /// <summary>
    /// Comma separated list of recipient email addresses
    /// </summary>
    [Column("EmailRecipients")]
    public string Recipients { get; set; }

    [Column("EmailSubject")]
    public string Subject { get; set; }

    [Column("EmailBody")]
    public string Body { get; set; }

    public List<string> GetRecipientsAsList() =>
        Recipients
        .Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries)
        .ToList();
}
