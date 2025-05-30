﻿using Biflow.Core.Interfaces;
using System.Text.Json.Serialization;

namespace Biflow.Core.Entities;

public sealed class ExecutionConditionParameter : ParameterBase, IAsyncEvaluable
{
    public ExecutionConditionParameter() { }

    internal ExecutionConditionParameter(ExecutionConditionParameter other, Step step, Job? job)
    {
        ParameterId = Guid.NewGuid();
        StepId = step.StepId;
        Step = step;
        ParameterName = other.ParameterName;
        ParameterValue = other.ParameterValue;

        // The target job is set, the JobParameter is not null and the target job has a parameter with a matching name.
        if (job is not null
            && other.JobParameter is not null
            && job.JobParameters.FirstOrDefault(p => p.ParameterName == other.JobParameter.ParameterName) is { } parameter)
        {
            JobParameterId = parameter.ParameterId;
            JobParameter = parameter;
        }
        // The target job has no parameter with a mathing name, so add one.
        else if (job is not null && other.JobParameter is not null)
        {
            var newParameter = new JobParameter(other.JobParameter, job);
            JobParameter = newParameter;
            JobParameterId = newParameter.ParameterId;
        }
        else
        {
            JobParameterId = other.JobParameterId;
            JobParameter = other.JobParameter;
        }
    }

    public Guid StepId { get; init; }

    [JsonIgnore]
    public Step Step { get; init; } = null!;

    public Guid? JobParameterId
    {
        get;
        set
        {
            field = value;
            if (field is not null)
            {
                ParameterValue = new();
            }
        }
    }

    [JsonIgnore]
    public JobParameter? JobParameter
    {
        get;
        set
        {
            field = value;
            if (field is not null)
            {
                ParameterValue = new();
            }
        }
    }

    public async Task<object?> EvaluateAsync()
    {
        if (JobParameter is not null)
        {
            return await JobParameter.EvaluateAsync();
        }

        return ParameterValue.Value;
    }

    [JsonIgnore]
    public override string DisplayValue => JobParameter switch
    {
        not null => $"{JobParameter.DisplayValue} (inherited from job parameter {JobParameter.DisplayName})",
        _ => base.DisplayValue
    };

    [JsonIgnore]
    public override string DisplayValueType => JobParameter?.DisplayValueType ?? base.DisplayValueType;
}
