using Biflow.Core.Interfaces;
using JetBrains.Annotations;

namespace Biflow.Core.Entities;

public class DbtStepExecution : StepExecution, IHasTimeout, IHasStepExecutionAttempts<DbtStepExecutionAttempt>
{
    public DbtStepExecution(string stepName) : base(stepName, StepType.Dbt)
    {
    }

    public DbtStepExecution(DbtStep step, Execution execution) : base(step, execution)
    {
        DbtJob = step.DbtJob;
        TimeoutMinutes = step.TimeoutMinutes;
        DbtAccountId = step.DbtAccountId;
        AddAttempt(new DbtStepExecutionAttempt(this));
    }

    public DbtJobDetails DbtJob { get; private set; } = new()
     {
         Id = 0,
         Name = null,
         EnvironmentId = 0,
         EnvironmentName = null,
         ProjectId = 0,
         ProjectName = null
     };

    public double TimeoutMinutes { get; [UsedImplicitly] private set; }

    public Guid DbtAccountId { get; [UsedImplicitly] private set; }

    public override DbtStepExecutionAttempt AddAttempt(StepExecutionStatus withStatus = default)
    {
        var previous = StepExecutionAttempts.MaxBy(x => x.RetryAttemptIndex);
        ArgumentNullException.ThrowIfNull(previous);
        var next = new DbtStepExecutionAttempt((DbtStepExecutionAttempt)previous, previous.RetryAttemptIndex + 1)
        {
            ExecutionStatus = withStatus
        };
        AddAttempt(next);
        return next;
    }

    /// <summary>
    /// Get the <see cref="DbtAccount"/> entity associated with this <see cref="StepExecution"/>.
    /// The method <see cref="SetAccount(DbtAccount?)"/> will need to have been called first for the <see cref="DbtAccount"/> to be available.
    /// </summary>
    /// <returns><see cref="DbtAccount"/> if it was previously set using <see cref="SetAccount(DbtAccount?)"/> with a non-null object; <see langword="null"/> otherwise.</returns>
    public DbtAccount? GetAccount() => _account;

    /// <summary>
    /// Set the private <see cref="DbtAccount"/> object used for containing a possible account reference.
    /// It can be later accessed using <see cref="GetAccount"/>.
    /// </summary>
    /// <param name="account"><see cref="DbtAccount"/> reference to store.
    /// The DbtAccountIds are compared and the value is set only if the ids match.</param>
    public void SetAccount(DbtAccount? account)
    {
        if (account?.DbtAccountId == DbtAccountId)
        {
            _account = account;
        }
    }

    // Use a field excluded from the EF model to store the account reference.
    // This is to avoid generating a foreign key constraint on the ExecutionStep table caused by a navigation property.
    // Make it private with public method access so that it is not used in EF Include method calls by accident.
    private DbtAccount? _account;
}
