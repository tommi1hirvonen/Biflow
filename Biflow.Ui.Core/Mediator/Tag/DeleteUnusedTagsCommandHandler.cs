using Biflow.DataAccess;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Biflow.Ui.Core;

internal class DeleteUnusedTagsCommandHandler(IDbContextFactory<AppDbContext> dbContextFactory)
    : IRequestHandler<DeleteUnusedTagsCommand, DeleteUnusedTagsResponse>
{
    public async Task<DeleteUnusedTagsResponse> Handle(DeleteUnusedTagsCommand request, CancellationToken cancellationToken)
    {
        using var context = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        var tags = await context.Tags
            .Where(t => !t.Steps.Any())
            .Where(t => !t.Schedules.Any())
            .Where(t => !t.JobSteps.Any())
            .Where(t => !t.TagSubscriptions.Any())
            .Where(t => !t.JobTagSubscriptions.Any())
            .ToArrayAsync(cancellationToken);
        context.Tags.RemoveRange(tags);
        await context.SaveChangesAsync(cancellationToken);
        return new(tags);
    }
}
