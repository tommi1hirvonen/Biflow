using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace EtlManager.Models
{
    public class JobExecution
    {
        [Key]
        public Guid ExecutionId { get; set; }

        [Display(Name = "Job")]
        public string JobName { get; set; }

        [Display(Name = "Created")]
        [DataType(DataType.DateTime)]
        public DateTime? CreatedDateTime { get; set; }

        [Display(Name = "Started")]
        [DataType(DataType.DateTime)]
        public DateTime? StartDateTime { get; set; }

        [Display(Name = "Ended")]
        [DataType(DataType.DateTime)]
        public DateTime? EndDateTime { get; set; }

        [Display(Name = "Status")]
        public string ExecutionStatus { get; set; }

        [Display(Name = "Duration (s)")]
        public decimal? ExecutionInSeconds { get; set; }
        [Display(Name = "Duration (min)")]
        public decimal? ExecutionInMinutes { get; set; }

        public bool DependencyMode { get; set; }

    }
}
