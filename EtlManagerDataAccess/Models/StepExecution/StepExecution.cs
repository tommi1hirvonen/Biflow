using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Security.Policy;
using System.Threading.Tasks;

namespace EtlManagerDataAccess.Models
{
    public abstract class StepExecution
    {
        public StepExecution(string stepName)
        {
            StepName = stepName;
        }

        [Display(Name = "Execution id")]
        public Guid ExecutionId { get; set; }

        [Display(Name = "Step id")]
        public Guid StepId { get; set; }

        [Display(Name = "Step")]
        public string StepName { get; set; }

        [Display(Name = "Step type")]
        public StepType? StepType { get; set; }

        public int ExecutionPhase { get; set; }

        public int RetryAttempts { get; set; }

        public int RetryIntervalMinutes { get; set; }

        public Execution Execution { get; set; } = null!;

        public ICollection<StepExecutionAttempt> StepExecutionAttempts { get; set; } = null!;
    }
}
