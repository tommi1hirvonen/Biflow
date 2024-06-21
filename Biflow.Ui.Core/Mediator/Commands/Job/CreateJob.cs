namespace Biflow.Ui.Core;

public record CreateJobCommand(Job Job) : IRequest;

internal class CreateJobCommandHandler(IDbContextFactory<AppDbContext> dbContextFactory)
    : IRequestHandler<CreateJobCommand>
{
    public async Task Handle(CreateJobCommand request, CancellationToken cancellationToken)
    {
        using var context = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        var tags = request.Job.Tags
            .Select(t => (t.TagName, t.Color))
            .Distinct()
            .ToArray();
        request.Job.Tags.Clear();
        var tagsFromDb = await context.JobTags
            .Where(t => tags.Select(t => t.TagName).Contains(t.TagName))
            .ToArrayAsync(cancellationToken);
        context.Jobs.Attach(request.Job).State = EntityState.Added;
        foreach (var (name, color) in tags)
        {
            var tag = tagsFromDb.FirstOrDefault(t => t.TagName == name) ?? new JobTag(name) { Color = color };
            request.Job.Tags.Add(tag);
        }
        await context.SaveChangesAsync(cancellationToken);
    }
}