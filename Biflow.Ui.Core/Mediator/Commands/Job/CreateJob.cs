namespace Biflow.Ui.Core;

public record CreateJobCommand(Job Job) : IRequest;

internal class CreateJobCommandHandler(IDbContextFactory<AppDbContext> dbContextFactory)
    : IRequestHandler<CreateJobCommand>
{
    public async Task Handle(CreateJobCommand request, CancellationToken cancellationToken)
    {
        using var context = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        var tagNames = request.Job.Tags
            .Select(t => t.TagName)
            .Distinct()
            .ToArray();
        request.Job.Tags.Clear();
        var tags = await context.JobTags
            .Where(t => tagNames.Contains(t.TagName))
            .ToArrayAsync(cancellationToken);
        context.Jobs.Attach(request.Job).State = EntityState.Added;
        foreach (var text in tagNames)
        {
            var tag = tags.FirstOrDefault(t => t.TagName == text) ?? new JobTag(text);
            request.Job.Tags.Add(tag);
        }
        await context.SaveChangesAsync(cancellationToken);
    }
}