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
        DatasetId = other.DatasetId;
    }

    [Required]
    public Guid AzureCredentialId { get; set; }

    [Display(Name = "Group id")]
    [MaxLength(36)]
    [MinLength(36)]
    [Required]
    public string WorkspaceId { get; set; } = "";

    [Display(Name = "Dataset id")]
    [MaxLength(36)]
    [MinLength(36)]
    [Required]
    public string DatasetId { get; set; } = "";

    [JsonIgnore]
    public AzureCredential? AzureCredential { get; set; }

    public override DatasetStep Copy(Job? targetJob = null) => new(this, targetJob);

    public override StepExecution ToStepExecution(Execution execution) => new DatasetStepExecution(this, execution);
}
