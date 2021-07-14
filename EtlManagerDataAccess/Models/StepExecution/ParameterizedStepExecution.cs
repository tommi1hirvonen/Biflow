using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Security.Policy;
using System.Threading.Tasks;

namespace EtlManagerDataAccess.Models
{
    public abstract record ParameterizedStepExecution : StepExecution
    {
        public ParameterizedStepExecution(string stepName) : base(stepName)
        {
        }

        public ICollection<StepExecutionParameter> StepExecutionParameters { get; set; } = null!;
    }
}
