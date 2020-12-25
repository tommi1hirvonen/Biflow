using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace EtlManagerUi.Models
{
    public class ParameterEdit : Parameter
    {
        public bool IsDeleted { get; set; } = false;
        public ParameterEdit()
        {

        }
        public ParameterEdit(Parameter parameter)
        {
            ParameterId = parameter.ParameterId;
            StepId = parameter.StepId;
            ParameterName = parameter.ParameterName;
            ParameterValue = parameter.ParameterValue;
        }
    }
}
