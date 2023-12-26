using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Biflow.DataAccess.Models;

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

    [Required]
    [MaxLength(20)]
    [Unicode(false)]
    public ParameterLevel ParameterLevel { get; set; } = ParameterLevel.Package;

    [JsonIgnore]
    public PackageStep Step { get; set; } = null!;

    [JsonIgnore]
    public override Step BaseStep => Step;

    [JsonIgnore]
    public override string DisplayName => $"${ParameterLevel}::{ParameterName}";
}
