using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Biflow.DataAccess.Models;

public class EmailStep : ParameterizedStep
{
    public EmailStep() : base(StepType.Email) { }

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

    public List<string> GetRecipientsAsList() =>
        Recipients
        .Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries)
        .ToList();
}
