using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace EtlManager.Models
{
    abstract public class Execution
    {
        abstract public Guid ExecutionId { get; set; }

        [Display(Name = "Job id")]
        public Guid JobId { get; set; }

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
        public int? ExecutionInSeconds { get; set; }

        [Display(Name = "Dependency mode")]
        public bool DependencyMode { get; set; }

        [Display(Name = "Created by")]
        public string CreatedBy { get; set; }

        public string GetDurationInReadableFormat()
        {
            if (ExecutionInSeconds == null) return null;
            var duration = TimeSpan.FromSeconds((double)ExecutionInSeconds);
            var result = "";
            var days = duration.Days;
            var hours = duration.Hours;
            var minutes = duration.Minutes;
            var seconds = duration.Seconds;
            if (days > 0) result += days + " d ";
            if (hours > 0 || days > 0) result += hours + " h ";
            if (minutes > 0 || hours > 0 || days > 0) result += minutes + " min ";
            result += seconds + " s";
            return result;
        }
    }
}
