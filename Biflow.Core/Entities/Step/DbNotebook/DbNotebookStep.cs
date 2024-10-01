using Biflow.Core.Interfaces;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Biflow.Core.Entities;

public class DbNotebookStep : Step, IHasTimeout, IHasStepParameters<DbNotebookStepParameter>
{
    [JsonConstructor]
    public DbNotebookStep() : base(StepType.DatabricksNotebook) { }

    private DbNotebookStep(DbNotebookStep other, Job? targetJob) : base(other, targetJob)
    {
        TimeoutMinutes = other.TimeoutMinutes;
        DatabricksWorkspaceId = other.DatabricksWorkspaceId;
        DatabricksWorkspace = other.DatabricksWorkspace;
        NotebookPath = other.NotebookPath;
        StepParameters = other.StepParameters
            .Select(p => new DbNotebookStepParameter(p, this, targetJob))
            .ToList();
    }

    [Required]
    [Range(0, 2880)] // 48 hours
    public double TimeoutMinutes { get; set; }

    [Required]
    public Guid DatabricksWorkspaceId { get; set; }

    [MaxLength(1000)]
    [Required]
    public string NotebookPath { get; set; } = "";

    [JsonIgnore]
    public DatabricksWorkspace? DatabricksWorkspace { get; set; }

    public ClusterConfiguration ClusterConfiguration { get; set; } = new NewClusterConfiguration();

    [ValidateComplexType]
    [JsonInclude]
    public IList<DbNotebookStepParameter> StepParameters { get; private set; } = new List<DbNotebookStepParameter>();

    public override DbNotebookStep Copy(Job? targetJob = null) => new(this, targetJob);

    public override StepExecution ToStepExecution(Execution execution) => new DbNotebookStepExecution(this, execution);
}
