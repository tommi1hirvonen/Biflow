namespace Biflow.Ui.Api.Mediator.Commands;

internal record CreateStepTagCommand(string TagName, TagColor Color, int SortOrder) : IRequest<StepTag>;

[UsedImplicitly]
internal class CreateStepTagCommandHandler(IDbContextFactory<ServiceDbContext> dbContextFactory)
    : IRequestHandler<CreateStepTagCommand, StepTag>
{
    public async Task<StepTag> Handle(CreateStepTagCommand request, CancellationToken cancellationToken)
    {
        await using var dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        var tag = new StepTag(request.TagName)
        {
            Color = request.Color,
            SortOrder = request.SortOrder
        };
        tag.EnsureDataAnnotationsValidated();
        dbContext.StepTags.Add(tag);
        await dbContext.SaveChangesAsync(cancellationToken);
        return tag;
    }
}