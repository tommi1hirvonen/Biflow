namespace Biflow.Ui.Api.Mediator.Commands;

internal record UpdateJobConcurrenciesCommand(Guid JobId, JobConcurrencyDto[] JobConcurrencies) : IRequest;

[UsedImplicitly]
internal class UpdateJobConcurrenciesCommandHandler(IDbContextFactory<ServiceDbContext> dbContextFactory)
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
                StepType = x.StepType,
                MaxParallelSteps = x.MaxParallelSteps
            })
            .ToArray();
        dbContext.MergeCollections(job.JobConcurrencies, newItems, x => new { x.JobId, x.StepType });
        foreach (var jobConcurrency in job.JobConcurrencies)
        {
            jobConcurrency.EnsureDataAnnotationsValidated();
        }
        await dbContext.SaveChangesAsync(cancellationToken);
    }
}