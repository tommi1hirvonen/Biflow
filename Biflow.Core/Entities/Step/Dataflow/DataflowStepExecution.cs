using Biflow.Core.Interfaces;
using System.ComponentModel.DataAnnotations;

namespace Biflow.Core.Entities;

public class DataflowStepExecution : StepExecution, IHasTimeout, IHasStepExecutionAttempts<DataflowStepExecutionAttempt>
{
    public DataflowStepExecution(string stepName, string dataflowGroupId, string dataflowId) : base(stepName, StepType.Dataflow)
    {
        DataflowGroupId = dataflowGroupId;
        DataflowId = dataflowId;
    }

    public DataflowStepExecution(DataflowStep step, Execution execution) : base(step, execution)
    {
        AzureCredentialId = step.AzureCredentialId;
        DataflowGroupId = step.DataflowGroupId;
        DataflowId = step.DataflowId;
        TimeoutMinutes = step.TimeoutMinutes;

        AddAttempt(new DataflowStepExecutionAttempt(this));
    }

    public Guid AzureCredentialId { get; private set; }

    [MinLength(36)]
    [MaxLength(36)]
    public string DataflowGroupId { get; private set; }

    [MinLength(36)]
    [MaxLength(36)]
    public string DataflowId { get; private set; }
    
    [Range(0, 2880)] // 48 hours
    public double TimeoutMinutes { get; set; }

    public override DataflowStepExecutionAttempt AddAttempt(StepExecutionStatus withStatus = default)
    {
        var previous = StepExecutionAttempts.MaxBy(x => x.RetryAttemptIndex);
        ArgumentNullException.ThrowIfNull(previous);
        var next = new DataflowStepExecutionAttempt((DataflowStepExecutionAttempt)previous, previous.RetryAttemptIndex + 1)
        {
            ExecutionStatus = withStatus
        };
        AddAttempt(next);
        return next;
    }

    /// <summary>
    /// Get the <see cref="AzureCredential"/> entity associated with this <see cref="StepExecution"/>.
    /// The method <see cref="SetAzureCredential(AzureCredential?)"/> will need to have been called first for the <see cref="AzureCredential"/> to be available.
    /// </summary>
    /// <returns><see cref="AzureCredential"/> if it was previously set using <see cref="SetAzureCredential(AzureCredential?)"/> with a non-null object; <see langword="null"/> otherwise.</returns>
    public AzureCredential? GetAzureCredential() => _azureCredential;

    /// <summary>
    /// Set the private <see cref="AzureCredential"/> object used for containing a possible app registration reference.
    /// It can be later accessed using <see cref="GetAzureCredential"/>.
    /// </summary>
    /// <param name="azureCredential"><see cref="AzureCredential"/> reference to store.
    /// The AzureCredentialIds are compared and the value is set only if the ids match.</param>
    public void SetAzureCredential(AzureCredential? azureCredential)
    {
        if (azureCredential?.AzureCredentialId == AzureCredentialId)
        {
            _azureCredential = azureCredential;
        }
    }

    // Use a field excluded from the EF model to store the Azure credential reference.
    // This is to avoid generating a foreign key constraint on the ExecutionStep table caused by a navigation property.
    // Make it private with public method access so that it is not used in EF Include method calls by accident.
    private AzureCredential? _azureCredential;
}