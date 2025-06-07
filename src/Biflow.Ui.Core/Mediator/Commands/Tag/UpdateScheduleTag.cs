namespace Biflow.Ui.Core;

public record UpdateScheduleTagCommand(Guid TagId, string TagName, TagColor Color, int SortOrder)
    : IRequest<ScheduleTag>;

[UsedImplicitly]
internal class UpdateScheduleTagCommandHandler(IDbContextFactory<AppDbContext> dbContextFactory)
    : IRequestHandler<UpdateScheduleTagCommand, ScheduleTag>
{
    public async Task<ScheduleTag> Handle(UpdateScheduleTagCommand request, CancellationToken cancellationToken)
    {
        await using var dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        var tag = await dbContext.ScheduleTags
                      .FirstOrDefaultAsync(t => t.TagId == request.TagId, cancellationToken)
                  ?? throw new NotFoundException<ScheduleTag>(request.TagId);
        tag.TagName = request.TagName;
        tag.Color = request.Color;
        tag.SortOrder = request.SortOrder;
        tag.EnsureDataAnnotationsValidated();
        await dbContext.SaveChangesAsync(cancellationToken);
        return tag;
    }
}