using Biflow.Core.Interfaces;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Biflow.Core.Entities;

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

    public double TimeoutMinutes { get; private set; }

    /// <summary>
    /// Get the <see cref="QlikCloudClient"/> entity associated with this <see cref="StepExecution"/>.
    /// The method <see cref="SetClient(QlikCloudClient?)"/> will need to have been called first for the <see cref="QlikCloudClient"/> to be available.
    /// </summary>
    /// <returns><see cref="QlikCloudClient"/> if it was previously set using <see cref="SetClient(QlikCloudClient?)"/> with a non-null object; <see langword="null"/> otherwise.</returns>
    public QlikCloudClient? GetClient() => _client;

    /// <summary>
    /// Set the private <see cref="QlikCloudClient"/> object used for containing a possible client reference.
    /// It can be later accessed using <see cref="GetClient"/>.
    /// </summary>
    /// <param name="client"><see cref="QlikCloudClient"/> reference to store.
    /// The QlikCloudClientIds are compared and the value is set only if the ids match.</param>
    public void SetClient(QlikCloudClient? client)
    {
        if (client?.QlikCloudClientId == QlikCloudClientId)
        {
            _client = client;
        }
    }

    // Use a field excluded from the EF model to store the client reference.
    // This is to avoid generating a foreign key constraint on the ExecutionStep table caused by a navigation property.
    // Make it private with public method access so that it is not used in EF Include method calls by accident.
    [NotMapped]
    private QlikCloudClient? _client;
}
