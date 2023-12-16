using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace Biflow.DataAccess.Models;

public class EmailStep : Step, IHasStepParameters<EmailStepParameter>
{
    [JsonConstructor]
    public EmailStep(Guid jobId) : base(StepType.Email, jobId) { }

    private EmailStep(EmailStep other, Job? targetJob) : base(other, targetJob)
    {
        Recipients = other.Recipients;
        Subject = other.Subject;
        Body = other.Body;
        StepParameters = other.StepParameters
            .Select(p => new EmailStepParameter(p, this, targetJob))
            .ToList();
    }

    /// <summary>
    /// Comma separated list of recipient email addresses
    /// </summary>
    [Column("EmailRecipients")]
    [Required]
    public string Recipients { get; set; } = string.Empty;

    [Column("EmailSubject")]
    [Required]
    public string Subject { get; set; } = string.Empty;

    [Column("EmailBody")]
    [Required]
    public string Body { get; set; } = string.Empty;

    [ValidateComplexType]
    public IList<EmailStepParameter> StepParameters { get; set; } = null!;

    public List<string> GetRecipientsAsList() =>
        Recipients
        .Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries)
        .ToList();

    internal override EmailStep Copy(Job? targetJob = null) => new(this, targetJob);

    internal override StepExecution ToStepExecution(Execution execution) => new EmailStepExecution(this, execution);
}
