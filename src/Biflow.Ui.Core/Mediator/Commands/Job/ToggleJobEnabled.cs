namespace Biflow.Ui.Core;

public record ToggleJobEnabledCommand(Guid JobId, bool IsEnabled) : IRequest;

[UsedImplicitly]
internal class ToggleJobEnabledCommandHandler(IDbContextFactory<AppDbContext> dbContextFactory)
    : IRequestHandler<ToggleJobEnabledCommand>
{
    public async Task Handle(ToggleJobEnabledCommand request, CancellationToken cancellationToken)
    {
        await using var context = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        var rowsUpdated = await context.Jobs
            .Where(job => job.JobId == request.JobId)
            .ExecuteUpdateAsync(x => x.SetProperty(job => job.IsEnabled, request.IsEnabled), cancellationToken);
        if (rowsUpdated == 0)
        {
            throw new NotFoundException<Job>(request.JobId);
        }
    }
}