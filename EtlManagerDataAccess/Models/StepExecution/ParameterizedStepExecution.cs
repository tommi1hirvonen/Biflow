using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Security.Policy;
using System.Threading.Tasks;

namespace EtlManagerDataAccess.Models
{
    public abstract class ParameterizedStepExecution : StepExecution
    {
        public ParameterizedStepExecution(string stepExecutionId, string stepName, string jobName, DateTime createdDateTime, string executionStatus)
            : base(stepExecutionId, stepName, jobName, createdDateTime, executionStatus)
        {
            StepExecutionId = stepExecutionId;
            StepName = stepName;
        }

        public ICollection<StepExecutionParameter> StepExecutionParameters { get; set; } = null!;
    }
}
