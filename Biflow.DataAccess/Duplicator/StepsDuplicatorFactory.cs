using Biflow.DataAccess.Models;
using Microsoft.EntityFrameworkCore;

namespace Biflow.DataAccess;

public class StepsDuplicatorFactory
{
    private readonly IDbContextFactory<BiflowContext> _dbContextFactory;

    public StepsDuplicatorFactory(IDbContextFactory<BiflowContext> dbContextFactory)
    {
        _dbContextFactory = dbContextFactory;
    }

    public Task<StepsDuplicator> CreateAsync(Guid stepId, Guid? targetJobId = null) =>
        CreateAsync(new[] { stepId }, targetJobId);

    public async Task<StepsDuplicator> CreateAsync(Guid[] stepIds, Guid? targetJobId = null)
    {
        var context = await _dbContextFactory.CreateDbContextAsync();
        var steps = await context.Steps
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
            .Where(s => stepIds.Contains(s.StepId))
            .ToArrayAsync();
        var targetJob = targetJobId is Guid id
            ? await context.Jobs.Include(j => j.JobParameters).FirstOrDefaultAsync(j => j.JobId == id)
            : null;
        var copies = steps.Select(s => s.Copy(targetJob)).ToList();
        return new StepsDuplicator(context, copies);
    }
}
