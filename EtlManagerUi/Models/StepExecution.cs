using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Security.Policy;
using System.Threading.Tasks;

namespace EtlManagerUi.Models
{
    public class StepExecution : Execution
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

        [Display(Name = "SQL statement")]
        public string? SqlStatement { get; set; }

        [Display(Name = "Package path")]
        public string? PackagePath { get; set; }

        [Display(Name = "Error message")]
        public string? ErrorMessage { get; set; }

        [Display(Name = "Info message")]
        public string? InfoMessage { get; set; }

        [Display(Name = "32 bit mode")]
        public bool ExecuteIn32BitMode { get; set; }

        [Display(Name = "Execute as login")]
        public string? ExecuteAsLogin { get; set; }

        [Display(Name = "Job to execute")]
        public Guid? JobToExecuteId { get; set; }

        [Display(Name = "Synchronized")]
        public bool? JobExecuteSynchronized { get; set; }
        [Display(Name = "File path")]
        public string? ExeFileName { get; set; }
        [Display(Name = "Arguments")]
        public string? ExeArguments { get; set; }
        [Display(Name = "Working directory")]
        public string? ExeWorkingDirectory { get; set; }
        [Display(Name = "Success exit code")]
        public int? ExeSuccessExitCode { get; set; }

        [Display(Name = "Power BI Service id")]
        public Guid? PowerBIServiceId { get; set; }
        
        [Display(Name = "Group id")]
        public string? DatasetGroupId { get; set; }

        [Display(Name = "Dataset id")]
        public string? DatasetId { get; set; }

        [Display(Name = "Pipeline name")]
        public string? PipelineName { get; set; }

        [Display(Name = "Data Factory id")]
        public Guid? DataFactoryId { get; set; }

        [Display(Name = "Pipeline run id")]
        public string? PipelineRunId { get; set; }

        public int RetryAttemptIndex { get; set; }

        public int RetryAttempts { get; set; }

        public int RetryIntervalMinutes { get; set; }

        [Display(Name = "Executor PID")]
        public int? ExecutorProcessId { get; set; }

        public long? PackageOperationId { get; set; }

        [Display(Name = "Stopped by")]
        public string? StoppedBy { get; set; }

        public ICollection<StepExecutionParameter> StepExecutionParameters { get; set; } = null!;
    }
}
