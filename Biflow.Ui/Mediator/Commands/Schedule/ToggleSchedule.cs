namespace Biflow.Ui;

public record ToggleScheduleCommand(Guid ScheduleId, bool IsEnabled) : IRequest;

internal class ToggleScheduleCommandHandler(
    IDbContextFactory<AppDbContext> dbContextFactory,
    ISchedulerService scheduler)
    : IRequestHandler<ToggleScheduleCommand>
{
    public async Task Handle(ToggleScheduleCommand request, CancellationToken cancellationToken)
    {
        await using var context = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        await using var transaction = context.Database.BeginTransaction();
        var schedule = await context.Schedules
            .FirstAsync(s => s.ScheduleId == request.ScheduleId, cancellationToken);
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