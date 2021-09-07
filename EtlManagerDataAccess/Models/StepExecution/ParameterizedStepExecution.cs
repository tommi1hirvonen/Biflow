using System.Collections.Generic;

namespace EtlManagerDataAccess.Models
{
    public abstract class ParameterizedStepExecution : StepExecution
    {
        public ParameterizedStepExecution(string stepName, StepType stepType) : base(stepName, stepType)
        {
        }

        public ICollection<StepExecutionParameter> StepExecutionParameters { get; set; } = null!;
    }
}
