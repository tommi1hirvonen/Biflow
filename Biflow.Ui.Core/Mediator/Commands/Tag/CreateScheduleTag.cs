namespace Biflow.Ui.Core;

public record CreateScheduleTagCommand(string TagName, TagColor Color, int SortOrder) : IRequest<ScheduleTag>;

[UsedImplicitly]
internal class CreateScheduleTagCommandHandler(IDbContextFactory<ServiceDbContext> dbContextFactory)
    : IRequestHandler<CreateScheduleTagCommand, ScheduleTag>
{
    public async Task<ScheduleTag> Handle(CreateScheduleTagCommand request, CancellationToken cancellationToken)
    {
        await using var dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        var tag = new ScheduleTag(request.TagName)
        {
            Color = request.Color,
            SortOrder = request.SortOrder
        };
        tag.EnsureDataAnnotationsValidated();
        dbContext.ScheduleTags.Add(tag);
        await dbContext.SaveChangesAsync(cancellationToken);
        return tag;
    }
}