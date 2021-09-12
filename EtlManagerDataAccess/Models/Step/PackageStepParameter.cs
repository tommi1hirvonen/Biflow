using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace EtlManagerDataAccess.Models
{
    public class PackageStepParameter : StepParameterBase
    {
        public PackageStepParameter(ParameterLevel parameterLevel) : base(ParameterType.Package)
        {
            ParameterLevel = parameterLevel;
        }

        [Required]
        public ParameterLevel ParameterLevel { get; set; }
    }
}
