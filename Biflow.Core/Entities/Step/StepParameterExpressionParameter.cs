﻿using Biflow.Core.Interfaces;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Biflow.Core.Entities;

public class StepParameterExpressionParameter : IAsyncEvaluable, IExpressionParameter<JobParameter>
{
    public StepParameterExpressionParameter() { }

    internal StepParameterExpressionParameter(StepParameterExpressionParameter other, StepParameterBase stepParameter, Job? job)
    {
        StepParameterId = stepParameter.ParameterId;
        StepParameter = stepParameter;
        ParameterId = Guid.NewGuid();
        ParameterName = other.ParameterName;

        // The target job is set and the target job has a parameter with a matching name.
        if (job?.JobParameters.FirstOrDefault(p => p.ParameterName == other.InheritFromJobParameter.ParameterName) is { } parameter)
        {
            InheritFromJobParameterId = parameter.ParameterId;
            InheritFromJobParameter = parameter;
        }
        // The target job has no parameter with a mathing name, so add one.
        else if (job is not null)
        {
            var newParameter = new JobParameter(other.InheritFromJobParameter, job);
            InheritFromJobParameter = newParameter;
            InheritFromJobParameterId = newParameter.ParameterId;
        }
        else
        {
            InheritFromJobParameterId = other.InheritFromJobParameterId;
            InheritFromJobParameter = other.InheritFromJobParameter;
        }
    }

    public Guid StepParameterId { get; init; }

    [JsonIgnore]
    public StepParameterBase StepParameter { get; init; } = null!;

    public Guid ParameterId { get; init; }

    [Required]
    [MinLength(1)]
    [MaxLength(128)]
    public string ParameterName { get; set; } = "";

    public Guid InheritFromJobParameterId { get; set; }

    [JsonIgnore]
    public JobParameter InheritFromJobParameter { get; set; } = null!;

    public async Task<object?> EvaluateAsync() => await InheritFromJobParameter.EvaluateAsync();
}
