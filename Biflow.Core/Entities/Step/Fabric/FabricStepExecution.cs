using System.ComponentModel.DataAnnotations;
using Biflow.Core.Interfaces;

namespace Biflow.Core.Entities;

public class FabricStepExecution : StepExecution,
    IHasTimeout,
    IHasStepExecutionParameters<FabricStepExecutionParameter>,
    IHasStepExecutionAttempts<FabricStepExecutionAttempt>
{
    public FabricStepExecution(string stepName) : base(stepName, StepType.Fabric)
    {
    }

    public FabricStepExecution(FabricStep step, Execution execution) : base(step, execution)
    {
        WorkspaceId = step.WorkspaceId;
        WorkspaceName = step.WorkspaceName;
        ItemType = step.ItemType;
        ItemId = step.ItemId;
        ItemName = step.ItemName;
        TimeoutMinutes = step.TimeoutMinutes;
        AzureCredentialId = step.AzureCredentialId;
        StepExecutionParameters = step.StepParameters
            .Select(p => new FabricStepExecutionParameter(p, this))
            .ToArray();
        AddAttempt(new FabricStepExecutionAttempt(this));
    }
    
    public Guid WorkspaceId { get; private set; }
    
    public string? WorkspaceName { get; private set; }
    
    public FabricItemType ItemType { get; private set; }
    
    public Guid ItemId { get; private set; }
    
    [MaxLength(250)]
    public string? ItemName { get; private set; }
    
    public double TimeoutMinutes { get; private set; }
    
    public Guid AzureCredentialId { get; private set; }
    
    public IEnumerable<FabricStepExecutionParameter> StepExecutionParameters { get; } =
        new List<FabricStepExecutionParameter>();
    
    public override FabricStepExecutionAttempt AddAttempt(StepExecutionStatus withStatus = default)
    {
        var previous = StepExecutionAttempts.MaxBy(x => x.RetryAttemptIndex);
        ArgumentNullException.ThrowIfNull(previous);
        var next = new FabricStepExecutionAttempt((FabricStepExecutionAttempt)previous, previous.RetryAttemptIndex + 1)
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