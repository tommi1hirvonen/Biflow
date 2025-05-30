﻿using System.Text.Json.Serialization;

namespace Biflow.Core.Entities;

public class PackageStepExecutionParameter : StepExecutionParameterBase
{
    public PackageStepExecutionParameter(string parameterName, ParameterValue parameterValue)
        : base(parameterName, parameterValue, ParameterType.Package)
    {

    }

    public PackageStepExecutionParameter(PackageStepParameter parameter, PackageStepExecution execution) : base(parameter, execution)
    {
        StepExecution = execution;
        ParameterLevel = parameter.ParameterLevel;
    }

    [JsonIgnore]
    public PackageStepExecution StepExecution { get; init; } = null!;

    [JsonIgnore]
    public override StepExecution BaseStepExecution => StepExecution;

    public ParameterLevel ParameterLevel { get; init; }

    [JsonIgnore]
    public override string DisplayName => $"${ParameterLevel}::{ParameterName}";
}
