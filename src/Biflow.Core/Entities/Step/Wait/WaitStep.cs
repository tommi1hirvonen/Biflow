using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Biflow.Core.Entities;

public class WaitStep : Step
{
    [JsonConstructor]
    public WaitStep() : base(StepType.Wait)
    {
    }

    private WaitStep(WaitStep other, Job? targetJob) : base(other, targetJob)
    {
        WaitSeconds = other.WaitSeconds;
    }

    [Range(1, 604800)] // up to 7 days
    public int WaitSeconds { get; set; }

    [JsonIgnore]
    public override DisplayStepType DisplayStepType => DisplayStepType.Wait;

    public override StepExecution ToStepExecution(Execution execution) => new WaitStepExecution(this, execution);

    public override WaitStep Copy(Job? targetJob = null) => new(this, targetJob);
}