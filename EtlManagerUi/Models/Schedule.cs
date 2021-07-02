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

        public string GetScheduleSummary()
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
            return NextFireTimesSequence().FirstOrDefault();
        }

        public IEnumerable<DateTime> GetNextFireTimes(int count)
        {
            return NextFireTimesSequence().Take(count);
        }

        private IEnumerable<DateTime> NextFireTimesSequence()
        {
            if (Quartz.CronExpression.IsValidExpression(CronExpression))
            {
                var cron = new CronExpression(CronExpression);
                DateTimeOffset? dateTime = DateTimeOffset.UtcNow;
                while (dateTime is not null)
                {
                    dateTime = cron.GetTimeAfter((DateTimeOffset)dateTime);
                    if (dateTime is null)
                        break;
                    else
                        yield return dateTime.Value.LocalDateTime;
                }
            }
        }
    }
}
