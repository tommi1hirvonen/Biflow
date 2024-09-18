namespace Biflow.Ui.Core;

public record UpdateJobCommand(Job Job) : IRequest;

internal class UpdateJobCommandHandler(IDbContextFactory<AppDbContext> dbContextFactory) : IRequestHandler<UpdateJobCommand>
{
    public async Task Handle(UpdateJobCommand request, CancellationToken cancellationToken)
    {
        using var context = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        
        var tagIds = request.Job.Tags
            .Select(t => t.TagId)
            .ToArray();        
        var tagsFromDb = await context.JobTags
            .Where(t => tagIds.Contains(t.TagId))
            .ToArrayAsync(cancellationToken);
        var jobFromDb = await context.Jobs
            .Include(j => j.Tags)
            .FirstOrDefaultAsync(j => j.JobId == request.Job.JobId, cancellationToken);
        
        if (jobFromDb is null)
        {
            return;
        }
        
        context.Entry(jobFromDb).CurrentValues.SetValues(request.Job);

        // Synchronize tags

        var tagsToAdd = request.Job.Tags
            .Where(t1 => !jobFromDb.Tags.Any(t2 => t2.TagId == t1.TagId))
            .Select(t => (t.TagId, t.TagName, t.Color));
        foreach (var (id, name, color) in tagsToAdd)
        {
            // New tag
            var tag = tagsFromDb.FirstOrDefault(t => t.TagId == id)
                ?? new JobTag(name) { Color = color };
            jobFromDb.Tags.Add(tag);
        }

        var tagsToRemove = jobFromDb.Tags
            .Where(t => !tagIds.Contains(t.TagId))
            .ToArray(); // materialize since items may be removed from the sequence
        foreach (var tag in tagsToRemove)
        {
            jobFromDb.Tags.Remove(tag);
        }

        await context.SaveChangesAsync(cancellationToken);
    }
}