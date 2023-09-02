using Biflow.DataAccess.Models;
using Microsoft.EntityFrameworkCore;

namespace Biflow.DataAccess;

public class StepDuplicatorFactory
{
    private readonly IDbContextFactory<BiflowContext> _dbContextFactory;

    internal StepDuplicatorFactory(IDbContextFactory<BiflowContext> dbContextFactory)
    {
        _dbContextFactory = dbContextFactory;
    }

    public async Task<StepDuplicator?> CreateAsync(Guid stepId, Guid? targetJobId = null)
    {
        var context = await _dbContextFactory.CreateDbContextAsync();
        var baseStep = await context.Steps
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
            .Include(nameof(SqlStep.ResultCaptureJobParameter))
            .FirstOrDefaultAsync(s => s.StepId == stepId);
        var targetJob = targetJobId is Guid id
            ? await context.Jobs.Include(j => j.JobParameters).FirstOrDefaultAsync(j => j.JobId == id)
            : null;
        if (baseStep is null)
        {
            return null;
        }
        var step = baseStep.Copy(targetJob);
        return new StepDuplicator(context, step);
    }
}
