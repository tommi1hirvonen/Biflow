using Biflow.Core.Interfaces;
using JetBrains.Annotations;

namespace Biflow.Core.Entities;

public class ScdStepExecution : StepExecution, IHasTimeout, IHasStepExecutionAttempts<ScdStepExecutionAttempt>
{
    public ScdStepExecution(string stepName) : base(stepName, StepType.Scd)
    {
    }

    public ScdStepExecution(ScdStep step, Execution execution) : base(step, execution)
    {
        TimeoutMinutes = step.TimeoutMinutes;
        ScdTableId = (Guid)step.ScdTableId!;
        AddAttempt(new ScdStepExecutionAttempt(this));
    }

    public double TimeoutMinutes { get; [UsedImplicitly] private set; }

    public Guid ScdTableId { get; [UsedImplicitly] private set; }
    
    public override DisplayStepType DisplayStepType => DisplayStepType.Scd;
    
    public override ScdStepExecutionAttempt AddAttempt(StepExecutionStatus withStatus = default)
    {
        var previous = StepExecutionAttempts.MaxBy(x => x.RetryAttemptIndex);
        ArgumentNullException.ThrowIfNull(previous);
        var next = new ScdStepExecutionAttempt((ScdStepExecutionAttempt)previous, previous.RetryAttemptIndex + 1)
        {
            ExecutionStatus = withStatus
        };
        AddAttempt(next);
        return next;
    }

    /// <summary>
    /// Get the <see cref="ScdTable"/> entity associated with this <see cref="StepExecution"/>.
    /// The method <see cref="SetScdTable(ScdTable?)"/> will need to have been called first for the <see cref="ScdTable"/> to be available.
    /// </summary>
    /// <returns><see cref="ScdTable"/> if it was previously set using <see cref="SetScdTable(ScdTable?)"/> with a non-null object; <see langword="null"/> otherwise.</returns>
    public ScdTable? GetScdTable() => _table;

    /// <summary>
    /// Set the private <see cref="ScdTable"/> object used for containing a possible SCD table reference.
    /// It can be later accessed using <see cref="GetScdTable"/>.
    /// </summary>
    /// <param name="table"><see cref="ScdTable"/> reference to store.
    /// The ScdTableIds are compared and the value is set only if the ids match.</param>
    public void SetScdTable(ScdTable? table)
    {
        if (table?.ScdTableId == ScdTableId)
        {
            _table = table;
        }
    }

    // Use a field excluded from the EF model to store the account reference.
    // This is to avoid generating a foreign key constraint on the ExecutionStep table caused by a navigation property.
    // Make it private with public method access so that it is not used in EF Include method calls by accident.
    private ScdTable? _table;
}
