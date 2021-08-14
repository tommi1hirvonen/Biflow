using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Security.Policy;
using System.Threading.Tasks;

namespace EtlManagerDataAccess.Models
{
    public record JobStepExecution : StepExecution
    {
        public JobStepExecution(string stepName) : base(stepName, StepType.Job)
        {
        }

        [Display(Name = "Job to execute")]
        public Guid JobToExecuteId { get; set; }

        [Display(Name = "Synchronized")]
        public bool JobExecuteSynchronized { get; set; }
    }
}
