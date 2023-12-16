using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace Biflow.DataAccess.Models;

public class SqlStep : Step, IHasConnection<SqlConnectionInfo>, IHasTimeout, IHasStepParameters<SqlStepParameter>
{
    [JsonConstructor]
    public SqlStep(Guid jobId) : base(StepType.Sql, jobId) { }

    private SqlStep(SqlStep other, Job? targetJob) : base(other, targetJob)
    {
        TimeoutMinutes = other.TimeoutMinutes;
        SqlStatement = other.SqlStatement;
        ConnectionId = other.ConnectionId;
        Connection = other.Connection;

        // The target job is set, the JobParameter is not null and the target job has a parameter with a matching name.
        if (targetJob is not null && other.ResultCaptureJobParameter is not null && targetJob.JobParameters.FirstOrDefault(p => p.ParameterName == other.ResultCaptureJobParameter.ParameterName) is JobParameter parameter)
        {
            ResultCaptureJobParameterId = parameter.ParameterId;
            ResultCaptureJobParameter = parameter;
        }
        // The target job has no parameter with a mathing name, so add one.
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

    [Column("TimeoutMinutes")]
    [Required]
    [Display(Name = "Timeout (min)")]
    [Range(0, 2880)] // 48 hours
    public double TimeoutMinutes { get; set; }

    [Display(Name = "SQL statement")]
    [Required]
    public string? SqlStatement { get; set; }

    [Column("ConnectionId")]
    [Required]
    public Guid? ConnectionId { get; set; }

    [Display(Name = "Result capture job parameter")]
    public Guid? ResultCaptureJobParameterId { get; set; }

    [JsonIgnore]
    public JobParameter? ResultCaptureJobParameter { get; set; }

    [JsonIgnore]
    public SqlConnectionInfo Connection { get; set; } = null!;

    [ValidateComplexType]
    public IList<SqlStepParameter> StepParameters { get; set; } = null!;

    internal override SqlStep Copy(Job? targetJob = null) => new(this, targetJob);

    internal override StepExecution ToStepExecution(Execution execution) => new SqlStepExecution(this, execution);
}
