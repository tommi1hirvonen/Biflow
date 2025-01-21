namespace Biflow.Ui.Api.Mediator.Commands;

internal record UpdateStepTagCommand(Guid TagId, string TagName, TagColor Color, int SortOrder) : IRequest<StepTag>;

[UsedImplicitly]
internal class UpdateStepTagCommandHandler(IDbContextFactory<ServiceDbContext> dbContextFactory)
    : IRequestHandler<UpdateStepTagCommand, StepTag>
{
    public async Task<StepTag> Handle(UpdateStepTagCommand request, CancellationToken cancellationToken)
    {
        await using var dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        var tag = await dbContext.StepTags
                      .FirstOrDefaultAsync(t => t.TagId == request.TagId, cancellationToken)
                  ?? throw new NotFoundException<StepTag>(request.TagId);
        dbContext.Entry(tag).CurrentValues.SetValues(request);
        tag.EnsureDataAnnotationsValidated();
        await dbContext.SaveChangesAsync(cancellationToken);
        return tag;
    }
}