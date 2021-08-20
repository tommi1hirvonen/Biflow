using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace EtlManagerDataAccess.Models
{
    public abstract record StepExecution
    {
        public StepExecution(string stepName, StepType stepType)
        {
            StepName = stepName;
            StepType = stepType;
        }

        [Display(Name = "Execution id")]
        public Guid ExecutionId { get; set; }

        [Display(Name = "Step id")]
        public Guid StepId { get; set; }

        [Display(Name = "Step")]
        public string StepName { get; set; }

        [Display(Name = "Step type")]
        public StepType StepType { get; private init; }

        public int ExecutionPhase { get; set; }

        public int RetryAttempts { get; set; }

        public int RetryIntervalMinutes { get; set; }

        public Execution Execution { get; set; } = null!;

        public Step? Step { get; set; }

        public ICollection<StepExecutionAttempt> StepExecutionAttempts { get; set; } = null!;

    }
}
