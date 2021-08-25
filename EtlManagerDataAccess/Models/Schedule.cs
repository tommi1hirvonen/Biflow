using System;
using System.ComponentModel.DataAnnotations;

namespace EtlManagerDataAccess.Models
{
    public class Schedule
    {
        [Key]
        public Guid ScheduleId { get; set; }
        
        [Required]
        public Guid JobId { get; set; }

        public Job Job { get; set; } = null!;
        
        [Required]
        [Display(Name = "Cron expression")]
        [CronExpression]
        public string? CronExpression { get; set; }
        
        
        [Required]
        [Display(Name = "Enabled")]
        public bool IsEnabled { get; set; }
        
        [Required]
        [Display(Name = "Created")]
        public DateTimeOffset CreatedDateTime { get; set; }
        
        [Display(Name = "Created by")]
        public string? CreatedBy { get; set; }

    }
}
