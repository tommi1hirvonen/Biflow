using Quartz;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace EtlManagerUi.Models
{
    public class Schedule
    {
        [Key]
        public Guid ScheduleId { get; set; }
        
        [Required]
        public Guid JobId { get; set; }
        
        public Job Job { get; set; }
        
        [Required]
        [Display(Name = "Cron expression")]
        [CronExpression]
        public string CronExpression { get; set; }
        
        
        [Required]
        [Display(Name = "Enabled")]
        public bool IsEnabled { get; set; }
        
        [Required]
        [Display(Name = "Created")]
        public DateTime CreatedDateTime { get; set; }
        
        [Display(Name = "Created by")]
        public string CreatedBy { get; set; }

        public string GetScheduleDescription()
        {
            if (Quartz.CronExpression.IsValidExpression(CronExpression))
            {
                var cron = new CronExpression(CronExpression);
                return cron.GetExpressionSummary();
            }
            else
            {
                return "Invalid Cron expression";
            }
        }

        public DateTime GetNextFireTime()
        {
            if (Quartz.CronExpression.IsValidExpression(CronExpression))
            {
                var cron = new CronExpression(CronExpression);
                var nextFireTime = cron.GetTimeAfter(DateTimeOffset.UtcNow)?.LocalDateTime;
                return nextFireTime ?? DateTime.MaxValue;
            }
            else
            {
                return DateTime.MaxValue;
            }
        }
    }
}
