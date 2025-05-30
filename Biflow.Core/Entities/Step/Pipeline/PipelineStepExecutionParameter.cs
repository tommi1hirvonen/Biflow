﻿using System.Text.Json.Serialization;

namespace Biflow.Core.Entities;

public class PipelineStepExecutionParameter : StepExecutionParameterBase
{
    public PipelineStepExecutionParameter(string parameterName, ParameterValue parameterValue)
        : base(parameterName, parameterValue, ParameterType.Pipeline)
    {

    }

    public PipelineStepExecutionParameter(PipelineStepParameter parameter, PipelineStepExecution execution) : base(parameter, execution)
    {
        StepExecution = execution;
    }

    [JsonIgnore]
    public PipelineStepExecution StepExecution { get; init; } = null!;

    [JsonIgnore]
    public override StepExecution BaseStepExecution => StepExecution;
}
