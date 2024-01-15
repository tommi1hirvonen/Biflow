using Biflow.Core.Interfaces;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Biflow.Core.Entities;

public class EmailStep : Step, IHasStepParameters<EmailStepParameter>
{
    [JsonConstructor]
    public EmailStep() : base(StepType.Email) { }

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
    [Required]
    public string Recipients { get; set; } = string.Empty;

    [Required]
    public string Subject { get; set; } = string.Empty;

    [Required]
    public string Body { get; set; } = string.Empty;

    [ValidateComplexType]
    public IList<EmailStepParameter> StepParameters { get; } = new List<EmailStepParameter>();

    public List<string> GetRecipientsAsList() =>
        Recipients
        .Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries)
        .ToList();

    public override EmailStep Copy(Job? targetJob = null) => new(this, targetJob);

    public override StepExecution ToStepExecution(Execution execution) => new EmailStepExecution(this, execution);
}
