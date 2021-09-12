using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EtlManagerDataAccess.Models
{
    public class ExecutionParameter
    {
        public ExecutionParameter(string parameterName, object parameterValue, ParameterValueType parameterValueType)
        {
            ParameterName = parameterName;
            ParameterValue = parameterValue;
            ParameterValueType = parameterValueType;
        }

        public Guid ExecutionId { get; set; }

        public Guid ParameterId { get; set; }

        public string ParameterName { get; set; }

        [Column(TypeName = "sql_variant")]
        public object ParameterValue { get; set; }

        public ParameterValueType ParameterValueType { get; set; } = ParameterValueType.String;

        public Execution Execution { get; set; } = null!;

        public ICollection<StepExecutionParameterBase> StepExecutionParameters { get; set; } = null!;
    }
}
