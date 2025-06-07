using Biflow.Core.Interfaces;
using System.ComponentModel.DataAnnotations;
using JetBrains.Annotations;

namespace Biflow.Core.Entities;

public class DatasetStepExecution : StepExecution, IHasStepExecutionAttempts<DatasetStepExecutionAttempt>
{
    public DatasetStepExecution(string stepName, string workspaceId, string datasetId) : base(stepName, StepType.Dataset)
    {
        WorkspaceId = workspaceId;
        DatasetId = datasetId;
    }

    public DatasetStepExecution(DatasetStep step, Execution execution) : base(step, execution)
    {
        AzureCredentialId = step.AzureCredentialId;
        WorkspaceId = step.WorkspaceId;
        WorkspaceName = step.WorkspaceName;
        DatasetId = step.DatasetId;
        DatasetName = step.DatasetName;

        AddAttempt(new DatasetStepExecutionAttempt(this));
    }

    public Guid AzureCredentialId { get; [UsedImplicitly] private set; }

    [MaxLength(36)]
    public string WorkspaceId { get; private set; }
    
    [MaxLength(250)]
    public string? WorkspaceName { get; private set; }

    [MaxLength(36)]
    public string DatasetId { get; private set; }
    
    [MaxLength(250)]
    public string? DatasetName { get; private set; }

    public override DatasetStepExecutionAttempt AddAttempt(StepExecutionStatus withStatus = default)
    {
        var previous = StepExecutionAttempts.MaxBy(x => x.RetryAttemptIndex);
        ArgumentNullException.ThrowIfNull(previous);
        var next = new DatasetStepExecutionAttempt((DatasetStepExecutionAttempt)previous, previous.RetryAttemptIndex + 1)
        {
            ExecutionStatus = withStatus
        };
        AddAttempt(next);
        return next;
    }

    /// <summary>
    /// Get the <see cref="AzureCredential"/> entity associated with this <see cref="StepExecution"/>.
    /// The method <see cref="SetAzureCredential"/> will need to have been called first for the <see cref="AzureCredential"/> to be available.
    /// </summary>
    /// <returns><see cref="AzureCredential"/> if it was previously set using <see cref="SetAzureCredential"/> with a non-null object; <see langword="null"/> otherwise.</returns>
    public AzureCredential? GetAzureCredential() => _azureCredential;

    /// <summary>
    /// Set the private <see cref="AzureCredential"/> object used for containing a possible Azure credential reference.
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
