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
            _parameterValue = parameterValue;
            ParameterType = parameterType;
            ParameterLevel = parameterLevel;
        }

        public Guid ExecutionId { get; set; }
        
        public Guid StepId { get; set; }

        public Guid ParameterId { get; set; }

        public string ParameterName { get; set; }

        [Column(TypeName = "sql_variant")]
        public object ParameterValue
        {
            get => ExecutionParameter?.ParameterValue ?? _parameterValue;
            set => _parameterValue = value;
        }

        private object _parameterValue;

        public string ParameterType { get; set; }

        public string ParameterLevel { get; set; }

        public Guid? ExecutionParameterId { get; set; }

        public ExecutionParameter? ExecutionParameter { get; set; }

        public ParameterizedStepExecution StepExecution { get; set; } = null!;
    }
}
