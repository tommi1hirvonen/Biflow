using Biflow.DataAccess;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Biflow.Ui.Core;

public record DeleteScheduleCommand(Guid ScheduleId) : IRequest;

internal class DeleteScheduleCommandHandler(
    IDbContextFactory<AppDbContext> dbContextFactory,
    ISchedulerService scheduler)
    : IRequestHandler<DeleteScheduleCommand>
{
    public async Task Handle(DeleteScheduleCommand request, CancellationToken cancellationToken)
    {
        using var context = dbContextFactory.CreateDbContext();
        using var transaction = context.Database.BeginTransaction();
        try
        {
            var schedule = await context.Schedules
                .Include(s => s.Tags)
                .FirstAsync(s => s.ScheduleId == request.ScheduleId, cancellationToken);
            context.Schedules.Remove(schedule);
            await context.SaveChangesAsync(cancellationToken);
            await scheduler.RemoveScheduleAsync(schedule);
            transaction.Commit();
        }
        catch
        {
            transaction.Rollback();
            throw;
        }
    }
}