namespace Biflow.Ui.Api.Mediator.Commands;

internal record UpdateJobTagCommand(Guid TagId, string TagName, TagColor Color, int SortOrder) : IRequest<JobTag>;

[UsedImplicitly]
internal class UpdateJobTagCommandHandler(IDbContextFactory<ServiceDbContext> dbContextFactory)
    : IRequestHandler<UpdateJobTagCommand, JobTag>
{
    public async Task<JobTag> Handle(UpdateJobTagCommand request, CancellationToken cancellationToken)
    {
        await using var dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        var tag = await dbContext.JobTags
            .FirstOrDefaultAsync(t => t.TagId == request.TagId, cancellationToken)
                ?? throw new NotFoundException<JobTag>(request.TagId);
        tag.TagName = request.TagName;
        tag.Color = request.Color;
        tag.SortOrder = request.SortOrder;
        tag.EnsureDataAnnotationsValidated();
        await dbContext.SaveChangesAsync(cancellationToken);
        return tag;
    }
}