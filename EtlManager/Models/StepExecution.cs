using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Security.Policy;
using System.Threading.Tasks;

namespace EtlManager.Models
{
    public class StepExecution : Execution
    {
        [Key]
        public string StepExecutionId { get; set; }

        [Display(Name = "Execution id")]
        override public Guid ExecutionId { get; set; }

        [Display(Name = "Step id")]
        public Guid StepId { get; set; }

        [Display(Name = "Step")]
        public string StepName { get; set; }

        [Display(Name = "Step type")]
        public string StepType { get; set; }

        [Display(Name = "SQL statement")]
        public string SqlStatement { get; set; }

        [Display(Name = "Package path")]
        public string PackagePath { get; set; }

        [Display(Name = "Error message")]
        public string ErrorMessage { get; set; }

        [Display(Name = "Info message")]
        public string InfoMessage { get; set; }

        [Display(Name = "32 bit mode")]
        public bool ExecuteIn32BitMode { get; set; }

        public int RetryAttemptIndex { get; set; }

        public int RetryAttempts { get; set; }

        public int RetryIntervalMinutes { get; set; }

        [Display(Name = "Executor PID")]
        public int? ExecutorProcessId { get; set; }

        public long? PackageOperationId { get; set; }

        [Display(Name = "Stopped by")]
        public string StoppedBy { get; set; }
    }
}
