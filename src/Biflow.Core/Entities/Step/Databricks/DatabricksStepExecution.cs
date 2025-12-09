using Biflow.Core.Interfaces;
using JetBrains.Annotations;

namespace Biflow.Core.Entities;

public class DatabricksStepExecution : StepExecution,
    IHasTimeout,
    IHasStepExecutionParameters<DatabricksStepExecutionParameter>,
    IHasStepExecutionAttempts<DatabricksStepExecutionAttempt>
{
    public DatabricksStepExecution(string stepName) : base(stepName, StepType.Databricks)
    {
    }

    public DatabricksStepExecution(DatabricksStep step, Execution execution) : base(step, execution)
    {
        DatabricksStepSettings = step.DatabricksStepSettings;
        DatabricksWorkspaceId = step.DatabricksWorkspaceId;
        TimeoutMinutes = step.TimeoutMinutes;
        StepExecutionParameters = step.StepParameters
            .Select(p => new DatabricksStepExecutionParameter(p, this))
            .ToArray();
        AddAttempt(new DatabricksStepExecutionAttempt(this));
    }

    public DatabricksStepSettings DatabricksStepSettings { get; init; } = new DbNotebookStepSettings();

    public Guid DatabricksWorkspaceId { get; [UsedImplicitly] private set; }

    public double TimeoutMinutes { get; [UsedImplicitly] private set; }

    public IEnumerable<DatabricksStepExecutionParameter> StepExecutionParameters { get; } = new List<DatabricksStepExecutionParameter>();

    public override DisplayStepType DisplayStepType => DatabricksStepSettings switch
    {
        DbNotebookStepSettings => DisplayStepType.DatabricksNotebook,
        DbPythonFileStepSettings => DisplayStepType.DatabricksPythonFile,
        DbJobStepSettings => DisplayStepType.DatabricksJob,
        DbPipelineStepSettings => DisplayStepType.DatabricksPipeline,
        _ => DisplayStepType.Databricks
    };
    
    public override DatabricksStepExecutionAttempt AddAttempt(StepExecutionStatus withStatus = default)
    {
        var previous = StepExecutionAttempts.MaxBy(x => x.RetryAttemptIndex);
        ArgumentNullException.ThrowIfNull(previous);
        var next = new DatabricksStepExecutionAttempt((DatabricksStepExecutionAttempt)previous, previous.RetryAttemptIndex + 1)
        {
            ExecutionStatus = withStatus
        };
        AddAttempt(next);
        return next;
    }

    /// <summary>
    /// Get the <see cref="DatabricksWorkspace"/> entity associated with this <see cref="StepExecution"/>.
    /// The method <see cref="SetWorkspace(DatabricksWorkspace?)"/> will need to have been called first for the <see cref="DatabricksWorkspace"/> to be available.
    /// </summary>
    /// <returns><see cref="DatabricksWorkspace"/> if it was previously set using <see cref="SetWorkspace(DatabricksWorkspace?)"/> with a non-null object; <see langword="null"/> otherwise.</returns>
    public DatabricksWorkspace? GetWorkspace() => _workspace;

    /// <summary>
    /// Set the private <see cref="DatabricksWorkspace"/> object used for containing a possible client reference.
    /// It can be later accessed using <see cref="GetWorkspace"/>.
    /// </summary>
    /// <param name="workspace"><see cref="DatabricksWorkspace"/> reference to store.
    /// The DatabricksNotebookClientIds are compared and the value is set only if the ids match.</param>
    public void SetWorkspace(DatabricksWorkspace? workspace)
    {
        if (workspace?.WorkspaceId == DatabricksWorkspaceId)
        {
            _workspace = workspace;
        }
    }

    // Use a field excluded from the EF model to store the client reference.
    // This is to avoid generating a foreign key constraint on the ExecutionStep table caused by a navigation property.
    // Make it private with public method access so that it is not used in EF Include method calls by accident.
    private DatabricksWorkspace? _workspace;
}