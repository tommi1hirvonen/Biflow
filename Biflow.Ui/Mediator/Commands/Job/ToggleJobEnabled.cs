namespace Biflow.Ui;

public record ToggleJobEnabledCommand(Guid JobId, bool IsEnabled) : IRequest;

internal class ToggleJobEnabledCommandHandler(IDbContextFactory<AppDbContext> dbContextFactory)
    : IRequestHandler<ToggleJobEnabledCommand>
{
    public async Task Handle(ToggleJobEnabledCommand request, CancellationToken cancellationToken)
    {
        await using var context = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        await context.Jobs
            .Where(job => job.JobId == request.JobId)
            .ExecuteUpdateAsync(x => x.SetProperty(job => job.IsEnabled, request.IsEnabled), cancellationToken);
    }
}