using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace EtlManagerUi.Models
{
    public class ScheduleGeneration
    {
        [Required]
        public Guid JobId { get; set; }

        [Required]
        [Display(Name = "Interval")]
        public string IntervalType { get; set; }

        [Required]
        [Range(1, 12)]
        [Display(Name = "Value")]
        public int IntervalValueHours { get; set; }

        [Required]
        [Display(Name = "Value")]
        public int IntervalValueMinutes { get; set; }

        [Required]
        [Display(Name = "Start time")]
        public string StartTime { get; set; }

        [Required]
        [Display(Name = "End time")]
        public string EndTime { get; set; }

        [Required]
        [Display(Name = "Mon")]
        public bool Monday { get; set; }
        [Required]
        [Display(Name = "Tue")]
        public bool Tuesday { get; set; }
        [Required]
        [Display(Name = "Wed")]
        public bool Wednesday { get; set; }
        [Required]
        [Display(Name = "Thu")]
        public bool Thursday { get; set; }
        [Required]
        [Display(Name = "Fri")]
        public bool Friday { get; set; }
        [Required]
        [Display(Name = "Sat")]
        public bool Saturday { get; set; }
        [Required]
        [Display(Name = "Sun")]
        public bool Sunday { get; set; }
    }
}
