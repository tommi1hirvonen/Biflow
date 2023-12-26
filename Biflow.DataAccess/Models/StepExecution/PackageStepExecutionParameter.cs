using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;

namespace Biflow.DataAccess.Models;

public class PackageStepExecutionParameter : StepExecutionParameterBase
{
    public PackageStepExecutionParameter(string parameterName, object parameterValue, ParameterValueType parameterValueType)
        : base(parameterName, parameterValue, ParameterType.Package, parameterValueType)
    {

    }

    public PackageStepExecutionParameter(PackageStepParameter parameter, PackageStepExecution execution) : base(parameter, execution)
    {
        StepExecution = execution;
        ParameterLevel = parameter.ParameterLevel;
    }

    public PackageStepExecution StepExecution { get; set; } = null!;

    public override StepExecution BaseStepExecution => StepExecution;

    [MaxLength(20)]
    [Unicode(false)]
    public ParameterLevel ParameterLevel { get; set; }

    public override string DisplayName => $"${ParameterLevel}::{ParameterName}";
}
