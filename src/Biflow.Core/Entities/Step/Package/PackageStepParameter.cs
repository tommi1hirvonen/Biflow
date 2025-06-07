using System.Text.Json.Serialization;

namespace Biflow.Core.Entities;

public class PackageStepParameter : StepParameterBase
{
    public PackageStepParameter() : base(ParameterType.Package)
    {
    }

    internal PackageStepParameter(PackageStepParameter other, PackageStep step, Job? job) : base(other, step, job)
    {
        ParameterLevel = other.ParameterLevel;
        Step = step;
    }

    public ParameterLevel ParameterLevel { get; set; } = ParameterLevel.Package;

    [JsonIgnore]
    public PackageStep Step { get; init; } = null!;

    [JsonIgnore]
    public override Step BaseStep => Step;

    [JsonIgnore]
    public override string DisplayName => $"${ParameterLevel}::{ParameterName}";
}
