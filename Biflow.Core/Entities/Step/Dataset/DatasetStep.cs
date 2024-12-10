using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Biflow.Core.Entities;

public class DatasetStep : Step
{
    [JsonConstructor]
    public DatasetStep() : base(StepType.Dataset) { }

    private DatasetStep(DatasetStep other, Job? targetJob) : base(other, targetJob)
    {
        AzureCredentialId = other.AzureCredentialId;
        AzureCredential = other.AzureCredential;
        WorkspaceId = other.WorkspaceId;
        WorkspaceName = other.WorkspaceName;
        DatasetId = other.DatasetId;
        DatasetName = other.DatasetName;
    }

    [Required]
    public Guid AzureCredentialId { get; set; }

    [MaxLength(36)]
    [MinLength(36)]
    [Required]
    public string WorkspaceId { get; set; } = "";
    
    [MaxLength(250)]
    public string? WorkspaceName { get; set; }

    [MaxLength(36)]
    [MinLength(36)]
    [Required]
    public string DatasetId { get; set; } = "";
    
    [MaxLength(250)]
    public string? DatasetName { get; set; }

    [JsonIgnore]
    public AzureCredential? AzureCredential { get; set; }

    public override DatasetStep Copy(Job? targetJob = null) => new(this, targetJob);

    public override StepExecution ToStepExecution(Execution execution) => new DatasetStepExecution(this, execution);
}
