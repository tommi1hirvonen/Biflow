using Biflow.Core.Interfaces;
using System.ComponentModel.DataAnnotations;
using JetBrains.Annotations;

namespace Biflow.Core.Entities;

public class DatasetStepExecution : StepExecution, IHasStepExecutionAttempts<DatasetStepExecutionAttempt>
{
    public DatasetStepExecution(string stepName, string datasetId, string datasetName)
        : base(stepName, StepType.Dataset)
    {
        DatasetId = datasetId;
        DatasetName = datasetName;
    }

    public DatasetStepExecution(DatasetStep step, Execution execution) : base(step, execution)
    {
        FabricWorkspaceId = step.FabricWorkspaceId;
        DatasetId = step.DatasetId;
        DatasetName = step.DatasetName;

        AddAttempt(new DatasetStepExecutionAttempt(this));
    }

    public Guid FabricWorkspaceId { get; [UsedImplicitly] private set; }

    [MaxLength(36)]
    [Required]
    public string DatasetId { get; private set; }
    
    [MaxLength(250)]
    [Required]
    public string DatasetName { get; private set; }
    
    public override DisplayStepType DisplayStepType => DisplayStepType.Dataset;

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
    /// Get the <see cref="FabricWorkspace"/> entity associated with this <see cref="StepExecution"/>.
    /// The method <see cref="SetFabricWorkspace(FabricWorkspace?)"/> will need to have been called first for the <see cref="FabricWorkspace"/> to be available.
    /// </summary>
    /// <returns><see cref="FabricWorkspace"/> if it was previously set using <see cref="SetFabricWorkspace(FabricWorkspace?)"/> with a non-null object; <see langword="null"/> otherwise.</returns>
    public FabricWorkspace? GetFabricWorkspace() => _fabricWorkspace;

    /// <summary>
    /// Set the private <see cref="FabricWorkspace"/> object used for containing a possible Fabric workspace reference.
    /// It can be later accessed using <see cref="GetFabricWorkspace"/>.
    /// </summary>
    /// <param name="fabricWorkspace"><see cref="FabricWorkspace"/> reference to store.
    /// The FabricWorkspaceIds are compared and the value is set only if the ids match.</param>
    public void SetFabricWorkspace(FabricWorkspace? fabricWorkspace)
    {
        if (fabricWorkspace?.FabricWorkspaceId == FabricWorkspaceId)
        {
            _fabricWorkspace = fabricWorkspace;
        }
    }

    // Use a field excluded from the EF model to store the Fabric workspace reference.
    // This is to avoid generating a foreign key constraint on the ExecutionStep table caused by a navigation property.
    // Make it private with public method access so that it is not used in EF Include method calls by accident.
    private FabricWorkspace? _fabricWorkspace;
}
