namespace EtlManagerExecutor
{
    public record Step(string StepId, string StepName);

    public record ExecutionPhaseStep(string StepId, string StepName, int ExecutionPhase)
        : Step(StepId, StepName);
}
