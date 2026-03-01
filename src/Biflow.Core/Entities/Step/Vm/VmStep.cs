using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Biflow.Core.Entities;

public class VmStep : Step
{
    [JsonConstructor]
    public VmStep() : base(StepType.Vm)
    {
    }

    private VmStep(VmStep other, Job? targetJob) : base(other, targetJob)
    {
        AzureCredentialId = other.AzureCredentialId;
        AzureCredential = other.AzureCredential;
        VirtualMachineResourceId = other.VirtualMachineResourceId;
        Operation = other.Operation;
    }

    [Required]
    public Guid AzureCredentialId { get; set; }

    [JsonIgnore]
    public AzureCredential? AzureCredential { get; set; }

    [Required]
    [MaxLength(2048)]
    public string VirtualMachineResourceId { get; set; } = "";

    [Required]
    public VmStepOperation Operation { get; set; } = VmStepOperation.EnsureRunning;

    [JsonIgnore]
    public override DisplayStepType DisplayStepType => Operation switch
    {
        VmStepOperation.EnsureRunning => DisplayStepType.VmEnsureRunning,
        VmStepOperation.EnsureStopped => DisplayStepType.VmEnsureStopped,
        _ => DisplayStepType.Vm
    };

    public override StepExecution ToStepExecution(Execution execution) => new VmStepExecution(this, execution);

    public override VmStep Copy(Job? targetJob = null) => new(this, targetJob);
}
