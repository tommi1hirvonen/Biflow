namespace Biflow.Ui.Core;

public record ToggleJobPinnedCommand(Guid JobId, bool IsPinned) : IRequest;

internal class ToggleJobPinnedCommandHandler(IDbContextFactory<AppDbContext> dbContextFactory)
    : IRequestHandler<ToggleJobPinnedCommand>
{
    public async Task Handle(ToggleJobPinnedCommand request, CancellationToken cancellationToken)
    {
        await using var context = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        await context.Jobs
            .Where(job => job.JobId == request.JobId)
            .ExecuteUpdateAsync(x => x.SetProperty(job => job.IsPinned, request.IsPinned), cancellationToken);
    }
}