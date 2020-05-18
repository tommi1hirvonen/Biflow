using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Security.Policy;
using System.Threading.Tasks;

namespace ExecutorManager.Models
{
    public class Execution
    {
        [Key]
        public string ExecutionId { get; set; }

        [Display(Name = "Job")]
        public string JobName { get; set; }

        [Display(Name = "Step")]
        public string StepName { get; set; }

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

        [Display(Name = "Step Type")]
        public string StepType { get; set; }

        [Display(Name = "SQL Statement")]
        public string SqlStatement { get; set; }

        [Display(Name = "Package Path")]
        public string PackagePath { get; set; }

        [Display(Name = "Error Message")]
        public string ErrorMessage { get; set; }

        [Display(Name = "32 Bit Mode")]
        public bool ExecuteIn32BitMode { get; set; }
    }
}
