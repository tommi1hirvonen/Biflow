namespace Biflow.Ui;

public record DeleteScheduleCommand(Guid ScheduleId) : IRequest;

internal class DeleteScheduleCommandHandler(
    IDbContextFactory<AppDbContext> dbContextFactory,
    ISchedulerService scheduler)
    : IRequestHandler<DeleteScheduleCommand>
{
    public async Task Handle(DeleteScheduleCommand request, CancellationToken cancellationToken)
    {
        await using var context = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        await using var transaction = await context.Database.BeginTransactionAsync(cancellationToken);
        try
        {
            var schedule = await context.Schedules
                .FirstAsync(s => s.ScheduleId == request.ScheduleId, cancellationToken);
            context.Schedules.Remove(schedule);
            await context.SaveChangesAsync(cancellationToken);
            await scheduler.RemoveScheduleAsync(schedule);
            await transaction.CommitAsync(cancellationToken);
        }
        catch
        {
            await transaction.RollbackAsync(CancellationToken.None);
            throw;
        }
    }
}