using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace EtlManagerDataAccess.Models
{
    public abstract class StepExecutionParameterBase
    {
        public StepExecutionParameterBase(string parameterName, object parameterValue, ParameterType parameterType, ParameterValueType parameterValueType)
        {
            ParameterName = parameterName;
            _parameterValue = parameterValue;
            ParameterValueType = parameterValueType;
            ParameterType = parameterType;
        }

        public Guid ExecutionId { get; set; }
        
        public Guid StepId { get; set; }

        public Guid ParameterId { get; set; }

        public ParameterType ParameterType { get; set; }

        public string ParameterName { get; set; }

        [Column(TypeName = "sql_variant")]
        public object ParameterValue
        {
            get => ExecutionParameter?.ParameterValue ?? _parameterValue;
            set => _parameterValue = value;
        }

        private object _parameterValue;

        public ParameterValueType ParameterValueType { get; set; } = ParameterValueType.String;

        public Guid? ExecutionParameterId { get; set; }

        public ExecutionParameter? ExecutionParameter { get; set; }

        public ParameterizedStepExecution StepExecution { get; set; } = null!;
    }
}
