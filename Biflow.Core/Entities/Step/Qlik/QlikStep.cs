using Biflow.Core.Interfaces;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Biflow.Core.Entities;

public class QlikStep : Step, IHasTimeout
{
    [JsonConstructor]
    public QlikStep() : base(StepType.Qlik)
    {
    }

    public QlikStep(QlikStep other, Job? targetJob) : base(other, targetJob)
    {
        QlikStepSettings = other.QlikStepSettings;
        QlikCloudEnvironmentId = other.QlikCloudEnvironmentId;
        QlikCloudEnvironment = other.QlikCloudEnvironment;
        TimeoutMinutes = other.TimeoutMinutes;
    }

    public QlikStepSettings QlikStepSettings { get; set; } = new QlikAppReloadSettings();

    [Required]
    public Guid QlikCloudEnvironmentId { get; set; }

    [JsonIgnore]
    public QlikCloudEnvironment QlikCloudEnvironment { get; set; } = null!;

    [Required]
    [Display(Name = "Timeout (min)")]
    [Range(0, 2880)] // 48 hours
    public double TimeoutMinutes { get; set; }

    public override QlikStep Copy(Job? targetJob = null) => new(this, targetJob);

    public override QlikStepExecution ToStepExecution(Execution execution) => new(this, execution);
}
