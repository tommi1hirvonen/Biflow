using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace EtlManagerUi.Models
{
    public class Job
    {
        [Key]
        [Required]
        public Guid JobId { get; set; }

        [Required]
        [MaxLength(250)]
        [Display(Name = "Job name")]
        public string JobName { get; set; }

        [Display(Name = "Description")]
        public string JobDescription { get; set; }

        [Required]
        [DataType(DataType.DateTime)]
        [Display(Name = "Created")]
        public DateTime CreatedDateTime { get; set; }
        
        [Required]
        [DataType(DataType.DateTime)]
        [Display(Name = "Last modified")]
        public DateTime LastModifiedDateTime { get; set; }

        [Required]
        [Display(Name = "Use dependency mode")]
        public bool UseDependencyMode { get; set; }

        [Required]
        [Display(Name = "Enabled")]
        public bool IsEnabled { get; set; }

        public ICollection<Step> Steps { get; set; }
        public ICollection<Schedule> Schedules { get; set; }

        public ICollection<Subscription> Subscriptions { get; set; }

        [Display(Name = "Created by")]
        public string CreatedBy { get; set; }

        [Display(Name = "Last modified by")]
        public string LastModifiedBy { get; set; }

        [Timestamp]
        public byte[] Timestamp { get; set; }
    }
}
