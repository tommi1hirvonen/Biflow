namespace Biflow.Ui.Api.Mediator.Commands;

public record CreateJobTagRelationCommand(Guid JobId, Guid TagId) : IRequest;

[UsedImplicitly]
internal class CreateJobTagRelationCommandHandler(IDbContextFactory<ServiceDbContext> dbContextFactory)
    : IRequestHandler<CreateJobTagRelationCommand>
{
    public async Task Handle(CreateJobTagRelationCommand request, CancellationToken cancellationToken)
    {
        await using var dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        var job = await dbContext.Jobs
            .Include(j => j.Tags)
            .FirstOrDefaultAsync(j => j.JobId == request.JobId, cancellationToken)
                  ?? throw new NotFoundException<Job>(request.JobId);
        var tag = await dbContext.JobTags.FirstOrDefaultAsync(t => t.TagId == request.TagId, cancellationToken)
            ?? throw new NotFoundException<JobTag>(request.TagId);
        job.Tags.Add(tag);
        await dbContext.SaveChangesAsync(cancellationToken);
    }
}