using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using EtlManagerUtils;

namespace EtlManagerDataAccess.Models
{
    public abstract record StepExecutionAttempt
    {
        public Guid ExecutionId { get; set; }
        
        public Guid StepId { get; set; }
        
        public int RetryAttemptIndex { get; set; }
        
        public DateTime? StartDateTime { get; set; }
        
        public DateTime? EndDateTime { get; set; }
        
        public StepExecutionStatus? ExecutionStatus { get; set; }

        public StepType? StepType { get; set; }

        [Display(Name = "Error message")]
        public string? ErrorMessage { get; set; }

        [Display(Name = "Stopped by")]
        public string? StoppedBy { get; set; }

        public StepExecution StepExecution { get; set; } = null!;

        [NotMapped]
        public string UniqueId => string.Concat(ExecutionId, StepId, RetryAttemptIndex);

        [NotMapped]
        public double? ExecutionInSeconds => ((EndDateTime ?? DateTime.Now) - StartDateTime)?.TotalSeconds;

        public string? GetDurationInReadableFormat() => ExecutionInSeconds?.SecondsToReadableFormat();
    }
}
