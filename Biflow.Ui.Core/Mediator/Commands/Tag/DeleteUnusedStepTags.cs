﻿namespace Biflow.Ui.Core;

public record DeleteUnusedStepTagsCommand : IRequest<DeleteUnusedStepTagsResponse>;

public record DeleteUnusedStepTagsResponse(IEnumerable<StepTag> DeletedTags);

[UsedImplicitly]
internal class DeleteUnusedStepTagsCommandHandler(IDbContextFactory<AppDbContext> dbContextFactory)
    : IRequestHandler<DeleteUnusedStepTagsCommand, DeleteUnusedStepTagsResponse>
{
    public async Task<DeleteUnusedStepTagsResponse> Handle(
        DeleteUnusedStepTagsCommand request, CancellationToken cancellationToken)
    {
        await using var context = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        var tags = await context.StepTags
            .Where(t => !t.Steps.Any())
            .Where(t => !t.Schedules.Any())
            .Where(t => !t.JobSteps.Any())
            .Where(t => !t.StepTagSubscriptions.Any())
            .Where(t => !t.JobStepTagSubscriptions.Any())
            .ToArrayAsync(cancellationToken);
        context.StepTags.RemoveRange(tags);
        await context.SaveChangesAsync(cancellationToken);
        return new(tags);
    }
}