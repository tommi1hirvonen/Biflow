namespace Biflow.Ui.Api.Mediator.Commands;

internal record CreateJobTagCommand(TagDto Tag) : IRequest<JobTag>;

[UsedImplicitly]
internal class CreateJobTagCommandHandler(IDbContextFactory<ServiceDbContext> dbContextFactory)
    : IRequestHandler<CreateJobTagCommand, JobTag>
{
    public async Task<JobTag> Handle(CreateJobTagCommand request, CancellationToken cancellationToken)
    {
        await using var dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        if (await dbContext.JobTags.AnyAsync(t => t.TagId == request.Tag.TagId, cancellationToken))
        {
            throw new PrimaryKeyException<JobTag>(request.Tag.TagId);
        }
        var tag = new JobTag(request.Tag.TagName)
        {
            TagId = request.Tag.TagId,
            Color = request.Tag.Color,
            SortOrder = request.Tag.SortOrder
        };
        tag.EnsureDataAnnotationsValidated();
        dbContext.JobTags.Add(tag);
        await dbContext.SaveChangesAsync(cancellationToken);
        return tag;
    }
}