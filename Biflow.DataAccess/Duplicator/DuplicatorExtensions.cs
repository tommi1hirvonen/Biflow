namespace Biflow.DataAccess;

internal static class DuplicatorExtensions
{
    internal static readonly string[] StepNavigationPaths =
    [
        nameof(Step.Job),
        nameof(Step.Dependencies),
        $"{nameof(Step.DataObjects)}.{nameof(StepDataObject.DataObject)}",
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
        nameof(QlikStep.QlikCloudClient),
        nameof(SqlStep.ResultCaptureJobParameter)
    ];
}
