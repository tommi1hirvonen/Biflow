using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace ExecutorManager.Models
{
    public class Schedule
    {
        [Key]
        public Guid ScheduleId { get; set; }
        [Required]
        public Guid JobId { get; set; }
        public Job Job { get; set; }
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
        [Required]
        [Range(0, 23, ErrorMessage = "Use values between 0 and 23")]
        [Display(Name = "Hours")]
        public int TimeHours { get; set; }
        [Required]
        [Display(Name = ":00")]
        public bool On00Minutes { get; set; }
        [Required]
        [Display(Name = ":15")]
        public bool On15Minutes { get; set; }
        [Required]
        [Display(Name = ":30")]
        public bool On30Minutes { get; set; }
        [Required]
        [Display(Name = ":45")]
        public bool On45Minutes { get; set; }

        public string GetMinutesText()
        {
            if (On00Minutes) return "00";
            else if (On15Minutes) return "15";
            else if (On30Minutes) return "30";
            else if (On45Minutes) return "45";
            else return "";
        }

    }
}
