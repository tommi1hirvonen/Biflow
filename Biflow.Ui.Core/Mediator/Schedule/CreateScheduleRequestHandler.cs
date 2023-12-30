using Biflow.DataAccess;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Biflow.Ui.Core;

internal class CreateScheduleRequestHandler(
    IDbContextFactory<AppDbContext> dbContextFactory,
    ISchedulerService scheduler)
    : IRequestHandler<CreateScheduleRequest>
{
    public async Task Handle(CreateScheduleRequest request, CancellationToken cancellationToken)
    {
        using var context = await dbContextFactory.CreateDbContextAsync(cancellationToken);

        // Synchronize tags
        request.Schedule.Tags.Clear();
        var tags = await context.Tags
            .Where(t => request.Tags.Contains(t.TagName))
            .ToListAsync(cancellationToken);
        foreach (var tagName in request.Tags)
        {
            var tag = tags.FirstOrDefault(t => t.TagName == tagName);
            if (tag is not null)
            {
                request.Schedule.Tags.Add(tag);
            }
        }

        using var transaction = context.Database.BeginTransaction();
        try
        {
            ArgumentNullException.ThrowIfNull(request.Schedule.Tags);
            context.Schedules.Add(request.Schedule);
            await context.SaveChangesAsync(cancellationToken);
            await scheduler.AddScheduleAsync(request.Schedule);
            transaction.Commit();
        }
        catch
        {
            transaction.Rollback();
            throw;
        }
    }
}
