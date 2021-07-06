using System;

namespace EtlManagerExecutor
{
    public record Step(Guid StepId, string StepName);

    public record ExecutionPhaseStep(Guid StepId, string StepName, int ExecutionPhase)
        : Step(StepId, StepName);
}
