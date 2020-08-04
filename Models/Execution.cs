using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Security.Policy;
using System.Threading.Tasks;

namespace EtlManager.Models
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
        public int? ExecutionInSeconds { get; set; }

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

        public string GetDurationInReadableFormat()
        {
            if (ExecutionInSeconds == null) return null;
            var duration = TimeSpan.FromSeconds((double)ExecutionInSeconds);
            var result = "";
            var days = duration.Days;
            var hours = duration.Hours;
            var minutes = duration.Minutes;
            var seconds = duration.Seconds;
            if (days > 0) result += days + "d ";
            if (hours > 0 || days > 0) result += hours + "h ";
            if (minutes > 0 || hours > 0 || days > 0) result += minutes + "min ";
            result += seconds + "s";
            return result;
        }
    }
}
