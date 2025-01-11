namespace Biflow.Ui.Api.Mediator.Commands;

internal record UpdateJobCommand(JobDto Job) : IRequest<Job>;

[UsedImplicitly]
internal class UpdateJobCommandHandler(IDbContextFactory<AppDbContext> dbContextFactory)
    : IRequestHandler<UpdateJobCommand, Job>
{
    public async Task<Job> Handle(UpdateJobCommand request, CancellationToken cancellationToken)
    {
        await using var context = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        var job = await context.Jobs
            .FirstOrDefaultAsync(j => j.JobId == request.Job.JobId, cancellationToken);
        if (job is null)
        {
            throw new NotFoundException<Job>(request.Job.JobId);
        }
        context.Entry(job).CurrentValues.SetValues(request.Job);
        job.EnsureDataAnnotationsValidated();
        await context.SaveChangesAsync(cancellationToken);
        return job;
    }
}