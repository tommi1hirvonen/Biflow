using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

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

    [JsonIgnore]
    public PackageStep Step { get; set; } = null!;

    [JsonIgnore]
    public override Step BaseStep => Step;

    [JsonIgnore]
    public override string DisplayName => $"${ParameterLevel}::{ParameterName}";
}
