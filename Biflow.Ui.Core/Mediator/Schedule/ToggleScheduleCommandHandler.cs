using Biflow.DataAccess;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Biflow.Ui.Core;

internal class ToggleScheduleCommandHandler(
    IDbContextFactory<AppDbContext> dbContextFactory,
    ISchedulerService scheduler)
    : IRequestHandler<ToggleScheduleCommand>
{
    public async Task Handle(ToggleScheduleCommand request, CancellationToken cancellationToken)
    {
        using var context = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        using var transaction = context.Database.BeginTransaction();
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