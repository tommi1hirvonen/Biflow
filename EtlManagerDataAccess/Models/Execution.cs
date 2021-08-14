using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using EtlManagerUtils;

namespace EtlManagerDataAccess.Models
{
    public class Execution
    {

        public Execution(string jobName, DateTime createdDateTime, ExecutionStatus executionStatus)
        {
            JobName = jobName;
            CreatedDateTime = createdDateTime;
            ExecutionStatus = executionStatus;
        }

        [Key]
        public Guid ExecutionId { get; set; }

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
        public ExecutionStatus ExecutionStatus { get; set; }

        [Display(Name = "Dependency mode")]
        public bool DependencyMode { get; set; }

        [Display(Name = "Created by")]
        public string? CreatedBy { get; set; }

        [Display(Name = "Schedule id")]
        public Guid? ScheduleId { get; set; }

        [Display(Name = "Executor PID")]
        public int? ExecutorProcessId { get; set; }

        public ICollection<StepExecution> StepExecutions { get; set; } = null!;

        public Job? Job { get; set; }

        [NotMapped]
        public double? ExecutionInSeconds => ((EndDateTime ?? DateTime.Now) - StartDateTime)?.TotalSeconds;

        public string? GetDurationInReadableFormat() => ExecutionInSeconds?.SecondsToReadableFormat();

        public decimal GetSuccessPercent()
        {
            var successCount = StepExecutions?.Count(step => step.StepExecutionAttempts?.Any(attempt => attempt.ExecutionStatus == StepExecutionStatus.Succeeded) ?? false) ?? 0;
            var allCount = StepExecutions?.Count ?? 0;
            return allCount > 0 ? (decimal)successCount / allCount * 100 : 0;
        }
    }
}
