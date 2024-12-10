using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using Biflow.Core.Interfaces;

namespace Biflow.Core.Entities;

public class DataflowStep : Step, IHasTimeout
{
    [JsonConstructor]
    public DataflowStep() : base(StepType.Dataflow) { }

    private DataflowStep(DataflowStep other, Job? targetJob) : base(other, targetJob)
    {
        AzureCredentialId = other.AzureCredentialId;
        AzureCredential = other.AzureCredential;
        WorkspaceId = other.WorkspaceId;
        DataflowId = other.DataflowId;
        TimeoutMinutes = other.TimeoutMinutes;
    }

    [Required]
    public Guid AzureCredentialId { get; set; }

    [MaxLength(36)]
    [MinLength(36)]
    [Required]
    public string WorkspaceId { get; set; } = "";

    [MaxLength(36)]
    [MinLength(36)]
    [Required]
    public string DataflowId { get; set; } = "";
    
    [Range(0, 2880)] // 48 hours
    public double TimeoutMinutes { get; set; }

    [JsonIgnore]
    public AzureCredential? AzureCredential { get; set; }

    public override DataflowStep Copy(Job? targetJob = null) => new(this, targetJob);

    public override StepExecution ToStepExecution(Execution execution) => new DataflowStepExecution(this, execution);
}