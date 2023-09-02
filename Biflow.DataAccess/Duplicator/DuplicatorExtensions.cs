using Biflow.DataAccess.Models;
using Microsoft.EntityFrameworkCore;

namespace Biflow.DataAccess;

internal static class DuplicatorExtensions
{
    internal static IQueryable<Step> IncludeNavigationPropertiesForDuplication(this IQueryable<Step> query) => query
        .Include(step => step.Job)
        .Include(step => step.Dependencies)
        .Include(step => step.Sources)
        .Include(step => step.Targets)
        .Include(step => step.Tags)
        .Include(step => step.ExecutionConditionParameters)
        .ThenInclude(p => p.JobParameter)
        .Include($"{nameof(IHasStepParameters.StepParameters)}.{nameof(StepParameterBase.InheritFromJobParameter)}")
        .Include($"{nameof(IHasStepParameters.StepParameters)}.{nameof(StepParameterBase.ExpressionParameters)}")
        .Include(step => step.ExecutionConditionParameters)
        .Include(nameof(IHasConnection.Connection))
        .Include(nameof(FunctionStep.FunctionApp))
        .Include(nameof(DatasetStep.AppRegistration))
        .Include(nameof(JobStep.JobToExecute))
        .Include(nameof(PipelineStep.PipelineClient))
        .Include(nameof(SqlStep.ResultCaptureJobParameter));
}
