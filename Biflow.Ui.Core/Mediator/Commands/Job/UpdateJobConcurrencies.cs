namespace Biflow.Ui.Core;

public record UpdateJobConcurrenciesCommand(Job Job) : IRequest;

internal class UpdateJobConcurrenciesCommandHandler(IDbContextFactory<AppDbContext> dbContextFactory)
    : IRequestHandler<UpdateJobConcurrenciesCommand>
{
    public async Task Handle(UpdateJobConcurrenciesCommand request, CancellationToken cancellationToken)
    {
        await using var context = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        var job = await context.Jobs
            .Include(j => j.JobConcurrencies)
            .FirstOrDefaultAsync(j => j.JobId == request.Job.JobId, cancellationToken);
        if (job is null)
        {
            return;
        }
        job.MaxParallelSteps = request.Job.MaxParallelSteps;
        context.MergeCollections(job.JobConcurrencies, request.Job.JobConcurrencies, c => c.StepType);
        await context.SaveChangesAsync(cancellationToken);
    }
}