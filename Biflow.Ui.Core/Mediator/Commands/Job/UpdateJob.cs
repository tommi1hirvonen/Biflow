namespace Biflow.Ui.Core;

public record UpdateJobCommand(Job Job) : IRequest;

internal class UpdateJobCommandHandler(IDbContextFactory<AppDbContext> dbContextFactory) : IRequestHandler<UpdateJobCommand>
{
    public async Task Handle(UpdateJobCommand request, CancellationToken cancellationToken)
    {
        using var context = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        
        var tagNames = request.Job.Tags
            .Select(t => t.TagName)
            .Distinct()
            .ToArray();
        
        var tags = await context.JobTags
            .Where(t => tagNames.Contains(t.TagName))
            .ToArrayAsync(cancellationToken);

        var job = await context.Jobs
            .Include(j => j.Tags)
            .FirstOrDefaultAsync(j => j.JobId == request.Job.JobId, cancellationToken);
        if (job is null)
        {
            return;
        }
        context.Entry(job).CurrentValues.SetValues(request.Job);

        // Synchronize tags
        foreach (var text in tagNames.Where(str => !job.Tags.Any(t => t.TagName == str)))
        {
            // New tags
            var tag = tags.FirstOrDefault(t => t.TagName == text) ?? new JobTag(text);
            job.Tags.Add(tag);
        }
        foreach (var tag in job.Tags.Where(t => !tagNames.Contains(t.TagName)).ToList() ?? [])
        {
            job.Tags.Remove(tag);
        }

        await context.SaveChangesAsync(cancellationToken);
    }
}