using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EtlManagerDataAccess.Models
{
    public class PackageStepExecutionParameter : StepExecutionParameterBase
    {
        public PackageStepExecutionParameter(string parameterName, object parameterValue, ParameterValueType parameterValueType)
            : base(parameterName, parameterValue, ParameterType.Package, parameterValueType)
        {

        }

        public ParameterLevel ParameterLevel { get; set; }
    }
}
