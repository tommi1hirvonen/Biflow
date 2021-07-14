using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace EtlManagerDataAccess.Models
{
    public class StepExecutionParameter
    {
        public StepExecutionParameter(string parameterName, object parameterValue, string parameterType, string parameterLevel)
        {
            ParameterName = parameterName;
            ParameterValue = parameterValue;
            ParameterType = parameterType;
            ParameterLevel = parameterLevel;
        }

        public Guid ExecutionId { get; set; }
        
        public Guid StepId { get; set; }

        public Guid ParameterId { get; set; }

        public string ParameterName { get; set; }

        [Column(TypeName = "sql_variant")]
        public object ParameterValue { get; set; }

        public string ParameterType { get; set; }

        public string ParameterLevel { get; set; }

        public ParameterizedStepExecution StepExecution { get; set; } = null!;
    }
}
