namespace Biflow.Ui.Core;

public record ToggleJobCommand(Guid JobId, bool IsEnabled) : IRequest;

internal class ToggleJobCommandHandler(IDbContextFactory<ServiceDbContext> dbContextFactory)
    : IRequestHandler<ToggleJobCommand>
{
    public async Task Handle(ToggleJobCommand request, CancellationToken cancellationToken)
    {
        using var context = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        await context.Jobs
            .Where(job => job.JobId == request.JobId)
            .ExecuteUpdateAsync(x => x.SetProperty(job => job.IsEnabled, request.IsEnabled), cancellationToken);
    }
}