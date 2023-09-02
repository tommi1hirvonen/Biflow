using Biflow.DataAccess.Models;

namespace Biflow.DataAccess;

internal static class DuplicatorExtensions
{
    internal static string[] StepNavigationPaths => new[]
    {
        nameof(Step.Job),
        nameof(Step.Dependencies),
        nameof(Step.Sources),
        nameof(Step.Targets),
        nameof(Step.Tags),
        $"{nameof(Step.ExecutionConditionParameters)}.{nameof(ExecutionConditionParameter.JobParameter)}",
        $"{nameof(IHasStepParameters.StepParameters)}.{nameof(StepParameterBase.InheritFromJobParameter)}",
        $"{nameof(IHasStepParameters.StepParameters)}.{nameof(StepParameterBase.ExpressionParameters)}",
        $"{nameof(JobStep.StepParameters)}.{nameof(JobStepParameter.AssignToJobParameter)}",
        nameof(IHasConnection.Connection),
        nameof(FunctionStep.FunctionApp),
        nameof(DatasetStep.AppRegistration),
        nameof(JobStep.JobToExecute),
        nameof(PipelineStep.PipelineClient),
        nameof(SqlStep.ResultCaptureJobParameter)
    };
}
