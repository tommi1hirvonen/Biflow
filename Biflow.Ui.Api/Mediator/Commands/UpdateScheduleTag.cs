namespace Biflow.Ui.Api.Mediator.Commands;

internal record UpdateScheduleTagCommand(Guid TagId, string TagName, TagColor Color, int SortOrder)
    : IRequest<ScheduleTag>;

[UsedImplicitly]
internal class UpdateScheduleTagCommandHandler(IDbContextFactory<ServiceDbContext> dbContextFactory)
    : IRequestHandler<UpdateScheduleTagCommand, ScheduleTag>
{
    public async Task<ScheduleTag> Handle(UpdateScheduleTagCommand request, CancellationToken cancellationToken)
    {
        await using var dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        var tag = await dbContext.ScheduleTags
                      .FirstOrDefaultAsync(t => t.TagId == request.TagId, cancellationToken)
                  ?? throw new NotFoundException<ScheduleTag>(request.TagId);
        dbContext.Entry(tag).CurrentValues.SetValues(request);
        tag.EnsureDataAnnotationsValidated();
        await dbContext.SaveChangesAsync(cancellationToken);
        return tag;
    }
}