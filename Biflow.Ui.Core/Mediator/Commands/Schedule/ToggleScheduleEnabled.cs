namespace Biflow.Ui.Core;

public record ToggleScheduleEnabledCommand(Guid ScheduleId, bool IsEnabled) : IRequest;

[UsedImplicitly]
internal class ToggleScheduleCommandHandler(
    IDbContextFactory<AppDbContext> dbContextFactory,
    ISchedulerService scheduler)
    : IRequestHandler<ToggleScheduleEnabledCommand>
{
    public async Task Handle(ToggleScheduleEnabledCommand request, CancellationToken cancellationToken)
    {
        await using var context = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        await using var transaction = await context.Database.BeginTransactionAsync(cancellationToken);
        var schedule = await context.Schedules
            .FirstOrDefaultAsync(s => s.ScheduleId == request.ScheduleId, cancellationToken)
            ?? throw new NotFoundException<Schedule>(request.ScheduleId);
        schedule.IsEnabled = request.IsEnabled;
        await context.SaveChangesAsync(cancellationToken);
        try
        {
            await scheduler.ToggleScheduleEnabledAsync(schedule, request.IsEnabled);
            await transaction.CommitAsync(CancellationToken.None);
        }
        catch
        {
            await transaction.RollbackAsync(CancellationToken.None);
            throw;
        }
    }
}