namespace Biflow.Ui.Api.Mediator.Commands;

internal record UpdateJobTagCommand(TagDto Tag) : IRequest<JobTag>;

[UsedImplicitly]
internal class UpdateJobTagCommandHandler(IDbContextFactory<ServiceDbContext> dbContextFactory)
    : IRequestHandler<UpdateJobTagCommand, JobTag>
{
    public async Task<JobTag> Handle(UpdateJobTagCommand request, CancellationToken cancellationToken)
    {
        await using var dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        var tag = await dbContext.JobTags
            .FirstOrDefaultAsync(t => t.TagId == request.Tag.TagId, cancellationToken)
                ?? throw new NotFoundException<JobTag>(request.Tag.TagId);
        dbContext.Entry(tag).CurrentValues.SetValues(request.Tag);
        tag.EnsureDataAnnotationsValidated();
        await dbContext.SaveChangesAsync(cancellationToken);
        return tag;
    }
}