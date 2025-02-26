namespace Biflow.Ui.Core;

public record CreateJobTagCommand(string TagName, TagColor Color, int SortOrder) : IRequest<JobTag>;

[UsedImplicitly]
internal class CreateJobTagCommandHandler(IDbContextFactory<ServiceDbContext> dbContextFactory)
    : IRequestHandler<CreateJobTagCommand, JobTag>
{
    public async Task<JobTag> Handle(CreateJobTagCommand request, CancellationToken cancellationToken)
    {
        await using var dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        var tag = new JobTag(request.TagName)
        {
            Color = request.Color,
            SortOrder = request.SortOrder
        };
        tag.EnsureDataAnnotationsValidated();
        dbContext.JobTags.Add(tag);
        await dbContext.SaveChangesAsync(cancellationToken);
        return tag;
    }
}