using Biflow.Core.Attributes.Validation;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Biflow.Core.Entities;

public class Schedule
{
    [JsonInclude]
    public Guid ScheduleId { get; private set; }

    [NotEmptyGuid]
    public Guid JobId { get; set; }

    [Required(AllowEmptyStrings = false)]
    [MaxLength(250)]
    public string ScheduleName { get; set; } = string.Empty;

    [JsonIgnore]
    public Job Job { get; set; } = null!;

    [Required]
    [Display(Name = "Cron expression")]
    [CronExpression]
    [MaxLength(200)]
    public string CronExpression { get; set; } = "";


    [Required]
    [Display(Name = "Enabled")]
    public bool IsEnabled { get; set; } = true;

    public bool DisallowConcurrentExecution { get; set; } = false;

    [Required]
    [Display(Name = "Created")]
    public DateTimeOffset CreatedOn { get; set; }

    [Display(Name = "Created by")]
    [MaxLength(250)]
    public string? CreatedBy { get; set; }

    public ICollection<StepTag> TagFilter { get; } = new List<StepTag>();
}
