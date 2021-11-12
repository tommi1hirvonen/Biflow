using System.ComponentModel.DataAnnotations;

namespace EtlManager.DataAccess.Models;

public class PackageStepParameter : StepParameterBase
{
    public PackageStepParameter(ParameterLevel parameterLevel) : base(ParameterType.Package)
    {
        ParameterLevel = parameterLevel;
    }

    [Required]
    public ParameterLevel ParameterLevel { get; set; }
}
