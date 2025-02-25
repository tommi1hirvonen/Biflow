namespace Biflow.Ui.Core;

public record UpdateJobConcurrenciesCommand(
    Guid JobId,
    int MaxParallelSteps,
    IDictionary<StepType, int> JobConcurrencies) : IRequest;

[UsedImplicitly]
internal class UpdateJobConcurrenciesCommandHandler(
    IDbContextFactory<ServiceDbContext> dbContextFactory,
    JobValidator jobValidator)
    : IRequestHandler<UpdateJobConcurrenciesCommand>
{
    public async Task Handle(UpdateJobConcurrenciesCommand request, CancellationToken cancellationToken)
    {
        await using var dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        var job = await dbContext.Jobs
            .Include(j => j.JobConcurrencies)
            .FirstOrDefaultAsync(j => j.JobId == request.JobId, cancellationToken)
                ?? throw new NotFoundException<Job>(request.JobId);
        var newItems = request.JobConcurrencies
            .Select(x => new JobConcurrency
            {
                JobId = request.JobId,
                StepType = x.Key,
                MaxParallelSteps = x.Value
            })
            .ToArray();
        job.MaxParallelSteps = request.MaxParallelSteps;
        dbContext.MergeCollections(job.JobConcurrencies, newItems, x => new { x.JobId, x.StepType });
        foreach (var jobConcurrency in job.JobConcurrencies)
        {
            jobConcurrency.EnsureDataAnnotationsValidated();
        }
        job.EnsureDataAnnotationsValidated();
        await jobValidator.EnsureValidatedAsync(job, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);
    }
}