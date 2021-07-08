using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;
using EtlManagerUtils;

namespace EtlManagerDataAccess.Models
{
    abstract public class Execution
    {

        public Execution(string jobName, DateTime createdDateTime, string executionStatus)
        {
            JobName = jobName;
            CreatedDateTime = createdDateTime;
            ExecutionStatus = executionStatus;
        }

        abstract public Guid ExecutionId { get; set; }

        [Display(Name = "Job id")]
        public Guid JobId { get; set; }

        [Display(Name = "Job")]
        public string JobName { get; set; }

        [Display(Name = "Created")]
        [DataType(DataType.DateTime)]
        public DateTime CreatedDateTime { get; set; }

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
        public string? CreatedBy { get; set; }

        [Display(Name = "Schedule id")]
        public Guid? ScheduleId { get; set; }

        public string? GetDurationInReadableFormat()
        {
            return ExecutionInSeconds?.SecondsToReadableFormat() ?? null;
        }
    }
}
