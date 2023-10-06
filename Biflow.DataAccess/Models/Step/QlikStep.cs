using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Biflow.DataAccess.Models;

public class QlikStep : Step, IHasTimeout
{
    public QlikStep(Guid jobId, string appId) : base(StepType.Qlik, jobId)
    {
        AppId = appId;
    }

    public QlikStep(QlikStep other, Job? targetJob) : base(other, targetJob)
    {
        AppId = other.AppId;
        QlikCloudClientId = other.QlikCloudClientId;
        QlikCloudClient = other.QlikCloudClient;
    }

    [Required]
    public string AppId { get; set; }

    [Required]
    public Guid QlikCloudClientId { get; set; }

    public QlikCloudClient QlikCloudClient { get; set; } = null!;

    [Column("TimeoutMinutes")]
    [Required]
    [Display(Name = "Timeout (min)")]
    [Range(0, 2880)] // 48 hours
    public double TimeoutMinutes { get; set; }

    internal override QlikStep Copy(Job? targetJob = null) => new(this, targetJob);

    internal override QlikStepExecution ToStepExecution(Execution execution) => new(this, execution);
}
