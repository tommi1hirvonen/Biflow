namespace Biflow.Ui.Core;

public record ToggleJobEnabledCommand(Guid JobId, bool IsEnabled) : IRequest;

internal class ToggleJobEnabledCommandHandler(IDbContextFactory<AppDbContext> dbContextFactory)
    : IRequestHandler<ToggleJobEnabledCommand>
{
    public async Task Handle(ToggleJobEnabledCommand request, CancellationToken cancellationToken)
    {
        using var context = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        await context.Jobs
            .Where(job => job.JobId == request.JobId)
            .ExecuteUpdateAsync(x => x.SetProperty(job => job.IsEnabled, request.IsEnabled), cancellationToken);
    }
}