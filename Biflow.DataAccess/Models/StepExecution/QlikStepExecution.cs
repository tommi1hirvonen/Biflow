using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Biflow.DataAccess.Models;

public class QlikStepExecution : StepExecution, IHasTimeout
{
    public QlikStepExecution(string stepName, string appId, Guid qlikCloudClientId) : base(stepName, StepType.Qlik)
    {
        AppId = appId;
        QlikCloudClientId = qlikCloudClientId;
    }

    public QlikStepExecution(QlikStep step, Execution execution) : base(step, execution)
    {
        AppId = step.AppId;
        QlikCloudClientId = step.QlikCloudClientId;
        TimeoutMinutes = step.TimeoutMinutes;
        StepExecutionAttempts = new[] { new QlikStepExecutionAttempt(this) };
    }

    [MaxLength(36)]
    public string AppId { get; private set; }

    public Guid QlikCloudClientId { get; private set; }

    [Column("TimeoutMinutes")]
    public double TimeoutMinutes { get; private set; }

    public QlikCloudClient QlikCloudClient { get; private set; } = null!;
}
