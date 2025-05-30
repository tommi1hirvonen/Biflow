﻿using Biflow.Core.Attributes.Validation;
using Biflow.Core.Interfaces;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Biflow.Core.Entities;

public class SqlStep : Step, IHasSqlConnection, IHasTimeout, IHasStepParameters<SqlStepParameter>
{
    [JsonConstructor]
    public SqlStep() : base(StepType.Sql) { }

    private SqlStep(SqlStep other, Job? targetJob) : base(other, targetJob)
    {
        TimeoutMinutes = other.TimeoutMinutes;
        SqlStatement = other.SqlStatement;
        ConnectionId = other.ConnectionId;
        Connection = other.Connection;

        // The target job is set, the JobParameter is not null and the target job has a parameter with a matching name.
        if (targetJob is not null
            && other.ResultCaptureJobParameter is not null
            && targetJob.JobParameters.FirstOrDefault(p => p.ParameterName == other.ResultCaptureJobParameter.ParameterName) is { } parameter)
        {
            ResultCaptureJobParameterId = parameter.ParameterId;
            ResultCaptureJobParameter = parameter;
        }
        // The target job has no parameter with a matching name, so add one.
        else if (targetJob is not null && other.ResultCaptureJobParameter is not null)
        {
            var newParameter = new JobParameter(other.ResultCaptureJobParameter, targetJob);
            ResultCaptureJobParameterId = newParameter.ParameterId;
            ResultCaptureJobParameter = newParameter;
        }
        else
        {
            ResultCaptureJobParameterId = other.ResultCaptureJobParameterId;
            ResultCaptureJobParameter = other.ResultCaptureJobParameter;
        }

        StepParameters = other.StepParameters
            .Select(p => new SqlStepParameter(p, this, targetJob))
            .ToList();
    }

    [Required]
    [Range(0, 2880)] // 48 hours
    public double TimeoutMinutes { get; set; }

    [Required]
    public string SqlStatement { get; set; } = "";

    [Required]
    [NotEmptyGuid]
    public Guid ConnectionId { get; set; }

    public Guid? ResultCaptureJobParameterId { get; set; }

    [JsonIgnore]
    public JobParameter? ResultCaptureJobParameter { get; init; }

    [JsonIgnore]
    public SqlConnectionBase Connection
    {
        get;
        set
        {
            if (value is MsSqlConnection or SnowflakeConnection)
            {
                field = value;
            }
            else
            {
                throw new ArgumentException($"Illegal connection type: {value.GetType().Name}. Connection must be of type {nameof(MsSqlConnection)} or {nameof(SnowflakeConnection)}.");
            }
        }
    } = null!;

    [ValidateComplexType]
    [JsonInclude]
    public IList<SqlStepParameter> StepParameters { get; private set; } = new List<SqlStepParameter>();

    public override SqlStep Copy(Job? targetJob = null) => new(this, targetJob);

    public override StepExecution ToStepExecution(Execution execution) => new SqlStepExecution(this, execution);
}
