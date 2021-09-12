using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EtlManagerDataAccess.Models
{
    public class StepExecutionParameter : StepExecutionParameterBase
    {
        public StepExecutionParameter(string parameterName, object parameterValue, ParameterValueType parameterValueType)
            : base(parameterName, parameterValue, ParameterType.Base, parameterValueType)
        {

        }
    }
}
