using Biflow.Core.Attributes.Validation;
using Biflow.Core.Interfaces;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Biflow.Core.Entities;

public class Schedule : IAuditable
{
    public Guid ScheduleId { get; init; }

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

    public bool DisallowConcurrentExecution { get; set; }

    [Required]
    [Display(Name = "Created")]
    public DateTimeOffset CreatedOn { get; set; }

    [Display(Name = "Created by")]
    [MaxLength(250)]
    public string? CreatedBy { get; set; }

    [Display(Name = "Last modified")]
    public DateTimeOffset LastModifiedOn { get; set; }

    [Display(Name = "Last modified by")]
    [MaxLength(250)]
    public string? LastModifiedBy { get; set; }

    [JsonInclude]
    public ICollection<StepTag> TagFilter { get; private set; } = new List<StepTag>();

    [JsonInclude]
    public ICollection<ScheduleTag> Tags { get; private set; } = new List<ScheduleTag>();
}
