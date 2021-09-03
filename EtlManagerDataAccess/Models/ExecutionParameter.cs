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
        public ExecutionParameter(string parameterName, object parameterValue, ParameterType parameterType)
        {
            ParameterName = parameterName;
            ParameterValue = parameterValue;
            ParameterType = parameterType;
        }

        public Guid ExecutionId { get; set; }

        public Guid ParameterId { get; set; }

        public string ParameterName { get; set; }

        [Column(TypeName = "sql_variant")]
        public object ParameterValue { get; set; }

        public ParameterType ParameterType { get; set; } = ParameterType.String;

        public Execution Execution { get; set; } = null!;

        public ICollection<StepExecutionParameter> StepExecutionParameters { get; set; } = null!;
    }
}
