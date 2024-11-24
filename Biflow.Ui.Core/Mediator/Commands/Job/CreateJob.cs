namespace Biflow.Ui.Core;

public record CreateJobCommand(Job Job) : IRequest;

internal class CreateJobCommandHandler(IDbContextFactory<AppDbContext> dbContextFactory)
    : IRequestHandler<CreateJobCommand>
{
    public async Task Handle(CreateJobCommand request, CancellationToken cancellationToken)
    {
        await using var context = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        var tags = request.Job.Tags
            .Select(t => (t.TagName, t.Color))
            .Distinct()
            .ToArray();
        request.Job.Tags.Clear();
        var tagsFromDb = await context.JobTags
            .Where(t1 => tags.Select(t2 => t2.TagName).Contains(t1.TagName))
            .ToArrayAsync(cancellationToken);
        context.Jobs.Attach(request.Job).State = EntityState.Added;
        foreach (var (name, color) in tags)
        {
            var tag = tagsFromDb.FirstOrDefault(t => t.TagName == name)
                ?? new JobTag(name) { Color = color };
            request.Job.Tags.Add(tag);
        }
        await context.SaveChangesAsync(cancellationToken);
    }
}