using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace Biflow.DataAccess.Models;

[Table("Schedule")]
public class Schedule
{
    [Key]
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
    public string CronExpression { get; set; } = "";


    [Required]
    [Display(Name = "Enabled")]
    public bool IsEnabled { get; set; } = true;

    public bool DisallowConcurrentExecution { get; set; } = false;

    [Required]
    [Display(Name = "Created")]
    public DateTimeOffset CreatedDateTime { get; set; }

    [Display(Name = "Created by")]
    public string? CreatedBy { get; set; }

    public IList<Tag> Tags { get; set; } = null!;
}
