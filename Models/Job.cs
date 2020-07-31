using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace EtlManager.Models
{
    public class Job
    {
        [Key]
        [Required]
        public Guid JobId { get; set; }

        [Required]
        [MaxLength(250)]
        [Display(Name = "Job Name")]
        public string JobName { get; set; }
        
        [Required]
        [DataType(DataType.DateTime)]
        [Display(Name = "Created")]
        public DateTime CreatedDateTime { get; set; }
        
        [Required]
        [DataType(DataType.DateTime)]
        [Display(Name = "Last Modified")]
        public DateTime LastModifiedDateTime { get; set; }

        [Required]
        [Display(Name = "Use Dependency Mode")]
        public bool UseDependencyMode { get; set; }

        public ICollection<Step> Steps { get; set; }
        public ICollection<Schedule> Schedules { get; set; }
    }
}
