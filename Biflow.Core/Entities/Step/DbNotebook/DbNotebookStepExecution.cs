using Biflow.Core.Interfaces;
using System.ComponentModel.DataAnnotations;

namespace Biflow.Core.Entities;

public class DbNotebookStepExecution : StepExecution,
    IHasTimeout,
    IHasStepExecutionParameters<DbNotebookStepExecutionParameter>,
    IHasStepExecutionAttempts<DbNotebookStepExecutionAttempt>
{
    public DbNotebookStepExecution(string stepName, string notebookPath) : base(stepName, StepType.DatabricksNotebook)
    {
        NotebookPath = notebookPath;
    }

    public DbNotebookStepExecution(DbNotebookStep step, Execution execution) : base(step, execution)
    {
        ArgumentNullException.ThrowIfNull(step.NotebookPath);
        ArgumentNullException.ThrowIfNull(step.DatabricksWorkspaceId);

        NotebookPath = step.NotebookPath;
        ClusterConfiguration = step.ClusterConfiguration;
        DatabricksWorkspaceId = step.DatabricksWorkspaceId;
        TimeoutMinutes = step.TimeoutMinutes;
        StepExecutionParameters = step.StepParameters
            .Select(p => new DbNotebookStepExecutionParameter(p, this))
            .ToArray();
        AddAttempt(new DbNotebookStepExecutionAttempt(this));
    }

    [MaxLength(1000)]
    public string NotebookPath { get; private set; }

    public ClusterConfiguration ClusterConfiguration { get; set; } = new NewClusterConfiguration();

    public Guid DatabricksWorkspaceId { get; private set; }

    public double TimeoutMinutes { get; private set; }

    public IEnumerable<DbNotebookStepExecutionParameter> StepExecutionParameters { get; } = new List<DbNotebookStepExecutionParameter>();

    public override DbNotebookStepExecutionAttempt AddAttempt(StepExecutionStatus withStatus = default)
    {
        var previous = StepExecutionAttempts.MaxBy(x => x.RetryAttemptIndex);
        ArgumentNullException.ThrowIfNull(previous);
        var next = new DbNotebookStepExecutionAttempt((DbNotebookStepExecutionAttempt)previous, previous.RetryAttemptIndex + 1)
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
    /// <param name="client"><see cref="DatabricksWorkspace"/> reference to store.
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