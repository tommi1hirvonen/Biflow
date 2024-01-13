using Biflow.Core.Interfaces;
using System.ComponentModel.DataAnnotations.Schema;

namespace Biflow.Core.Entities;

public class EmailStepExecution : StepExecution, IHasStepExecutionParameters<EmailStepExecutionParameter>
{
    public EmailStepExecution(string stepName, string recipients, string subject, string body) : base(stepName, StepType.Email)
    {
        Recipients = recipients;
        Subject = subject;
        Body = body;
    }

    public EmailStepExecution(EmailStep step, Execution execution) : base(step, execution)
    {
        Recipients = step.Recipients;
        Subject = step.Subject;
        Body = step.Body;

        StepExecutionParameters = step.StepParameters
            .Select(p => new EmailStepExecutionParameter(p, this))
            .ToArray();
        StepExecutionAttempts = new[] { new EmailStepExecutionAttempt(this) };
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
