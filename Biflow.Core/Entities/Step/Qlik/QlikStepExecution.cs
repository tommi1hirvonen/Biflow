using Biflow.Core.Interfaces;

namespace Biflow.Core.Entities;

public class QlikStepExecution : StepExecution, IHasTimeout, IHasStepExecutionAttempts<QlikStepExecutionAttempt>
{
    public QlikStepExecution(string stepName, Guid qlikCloudEnvironmentId) : base(stepName, StepType.Qlik)
    {
        QlikCloudEnvironmentId = qlikCloudEnvironmentId;
    }

    public QlikStepExecution(QlikStep step, Execution execution) : base(step, execution)
    {
        QlikStepSettings = step.QlikStepSettings;
        QlikCloudEnvironmentId = step.QlikCloudEnvironmentId;
        TimeoutMinutes = step.TimeoutMinutes;
        AddAttempt(new QlikStepExecutionAttempt(this));
    }

    public QlikStepSettings QlikStepSettings { get; private set; } = new QlikAppReloadSettings();

    public Guid QlikCloudEnvironmentId { get; private set; }

    public double TimeoutMinutes { get; private set; }

    public override QlikStepExecutionAttempt AddAttempt(StepExecutionStatus withStatus = default)
    {
        var previous = StepExecutionAttempts.MaxBy(x => x.RetryAttemptIndex);
        ArgumentNullException.ThrowIfNull(previous);
        var next = new QlikStepExecutionAttempt((QlikStepExecutionAttempt)previous, previous.RetryAttemptIndex + 1)
        {
            ExecutionStatus = withStatus
        };
        AddAttempt(next);
        return next;
    }

    /// <summary>
    /// Get the <see cref="QlikCloudEnvironment"/> entity associated with this <see cref="StepExecution"/>.
    /// The method <see cref="SetEnvironment(QlikCloudEnvironment?)"/> will need to have been called first for the <see cref="QlikCloudEnvironment"/> to be available.
    /// </summary>
    /// <returns><see cref="QlikCloudEnvironment"/> if it was previously set using <see cref="SetEnvironment(QlikCloudEnvironment?)"/> with a non-null object; <see langword="null"/> otherwise.</returns>
    public QlikCloudEnvironment? GetEnvironment() => _environment;

    /// <summary>
    /// Set the private <see cref="QlikCloudEnvironment"/> object used for containing a possible client reference.
    /// It can be later accessed using <see cref="GetEnvironment"/>.
    /// </summary>
    /// <param name="environment"><see cref="QlikCloudEnvironment"/> reference to store.
    /// The QlikCloudClientIds are compared and the value is set only if the ids match.</param>
    public void SetEnvironment(QlikCloudEnvironment? environment)
    {
        if (environment?.QlikCloudEnvironmentId == QlikCloudEnvironmentId)
        {
            _environment = environment;
        }
    }

    // Use a field excluded from the EF model to store the client reference.
    // This is to avoid generating a foreign key constraint on the ExecutionStep table caused by a navigation property.
    // Make it private with public method access so that it is not used in EF Include method calls by accident.
    private QlikCloudEnvironment? _environment;
}
