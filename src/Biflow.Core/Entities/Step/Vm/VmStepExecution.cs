using Biflow.Core.Interfaces;
using System.ComponentModel.DataAnnotations;
using JetBrains.Annotations;

namespace Biflow.Core.Entities;

public class VmStepExecution : StepExecution, IHasStepExecutionAttempts<VmStepExecutionAttempt>
{
    public VmStepExecution(string stepName) : base(stepName, StepType.Vm)
    {
    }

    public VmStepExecution(VmStep step, Execution execution) : base(step, execution)
    {
        AzureCredentialId = step.AzureCredentialId;
        VirtualMachineResourceId = step.VirtualMachineResourceId;
        Operation = step.Operation;
        AddAttempt(new VmStepExecutionAttempt(this));
    }

    public Guid AzureCredentialId { get; [UsedImplicitly] private set; }

    [MaxLength(2048)]
    public string VirtualMachineResourceId { get; [UsedImplicitly] private set; } = "";

    public VmStepOperation Operation { get; [UsedImplicitly] private set; }

    public override DisplayStepType DisplayStepType => Operation switch
    {
        VmStepOperation.EnsureRunning => DisplayStepType.VmEnsureRunning,
        VmStepOperation.EnsureStopped => DisplayStepType.VmEnsureStopped,
        _ => DisplayStepType.Vm
    };

    public override VmStepExecutionAttempt AddAttempt(StepExecutionStatus withStatus = default)
    {
        var previous = StepExecutionAttempts.MaxBy(x => x.RetryAttemptIndex);
        ArgumentNullException.ThrowIfNull(previous);
        var next = new VmStepExecutionAttempt((VmStepExecutionAttempt)previous, previous.RetryAttemptIndex + 1)
        {
            ExecutionStatus = withStatus
        };
        AddAttempt(next);
        return next;
    }

    public AzureCredential? GetAzureCredential() => _azureCredential;

    public void SetAzureCredential(AzureCredential? credential)
    {
        if (credential?.AzureCredentialId == AzureCredentialId)
        {
            _azureCredential = credential;
        }
    }

    private AzureCredential? _azureCredential;
}
