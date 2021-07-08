using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Security.Policy;
using System.Threading.Tasks;

namespace EtlManagerDataAccess.Models
{
    public abstract class StepExecution : Execution
    {
        public StepExecution(string stepExecutionId, string stepName, string jobName, DateTime createdDateTime, string executionStatus)
            : base(jobName, createdDateTime, executionStatus)
        {
            StepExecutionId = stepExecutionId;
            StepName = stepName;
        }

        [Key]
        public string StepExecutionId { get; set; }

        [Display(Name = "Execution id")]
        override public Guid ExecutionId { get; set; }

        [Display(Name = "Step id")]
        public Guid StepId { get; set; }

        [Display(Name = "Step")]
        public string StepName { get; set; }

        [Display(Name = "Step type")]
        public StepType? StepType { get; set; }

        [Display(Name = "Error message")]
        public string? ErrorMessage { get; set; }

        public int RetryAttemptIndex { get; set; }

        public int RetryAttempts { get; set; }

        public int RetryIntervalMinutes { get; set; }

        [Display(Name = "Executor PID")]
        public int? ExecutorProcessId { get; set; }

        [Display(Name = "Stopped by")]
        public string? StoppedBy { get; set; }
    }
}
