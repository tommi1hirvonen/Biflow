using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace EtlManager.Models
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
        [Display(Name = "Minutes")]
        public int TimeMinutes { get; set; } = 0;

        public string GetMinutesText()
        {
            string temp = "0" + TimeMinutes.ToString();
            return temp.Substring(temp.Length - 2);
        }
    }
}
