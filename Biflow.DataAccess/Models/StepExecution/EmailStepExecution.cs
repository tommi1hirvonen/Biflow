using System.ComponentModel.DataAnnotations.Schema;

namespace Biflow.DataAccess.Models;

public class EmailStepExecution : StepExecution, IHasStepExecutionParameters<EmailStepExecutionParameter>
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
    public string Recipients { get; private set; }

    [Column("EmailSubject")]
    public string Subject { get; private set; }

    [Column("EmailBody")]
    public string Body { get; private set; }

    public IList<EmailStepExecutionParameter> StepExecutionParameters { get; set; } = null!;

    public List<string> GetRecipientsAsList() =>
        Recipients
        .Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries)
        .ToList();

}
