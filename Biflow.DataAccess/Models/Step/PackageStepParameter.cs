using System.ComponentModel.DataAnnotations;

namespace Biflow.DataAccess.Models;

public class PackageStepParameter : StepParameterBase
{
    public PackageStepParameter(ParameterLevel parameterLevel) : base(ParameterType.Package)
    {
        ParameterLevel = parameterLevel;
    }

    internal PackageStepParameter(PackageStepParameter other, PackageStep step, Job? job) : base(other, step, job)
    {
        ParameterLevel = other.ParameterLevel;
        Step = step;
    }

    [Required]
    public ParameterLevel ParameterLevel { get; set; }

    public PackageStep Step { get; set; } = null!;

    public override Step BaseStep => Step;

    public override string DisplayName => $"${ParameterLevel}::{ParameterName}";
}
