namespace Biflow.Ui.Api.Mediator.Commands;

internal record CreateScheduleCommand(
    Guid JobId,
    string ScheduleName,
    string CronExpression,
    bool IsEnabled,
    bool DisallowConcurrentExecution,
    Guid[] ScheduleTagIds,
    Guid[] FilterStepTagIds) : IRequest<Schedule>;

[UsedImplicitly]
internal class CreateScheduleCommandHandler(
    IDbContextFactory<AppDbContext> dbContextFactory,
    ISchedulerService scheduler)
    : IRequestHandler<CreateScheduleCommand, Schedule>
{
    public async Task<Schedule> Handle(CreateScheduleCommand request, CancellationToken cancellationToken)
    {
        await using var dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken);

        if (!await dbContext.Jobs.AnyAsync(j => j.JobId == request.JobId, cancellationToken))
        {
            throw new NotFoundException<Job>(request.JobId);
        }
        
        var scheduleTags = await dbContext.ScheduleTags
            .Where(t => request.ScheduleTagIds.Contains(t.TagId))
            .ToArrayAsync(cancellationToken);

        foreach (var id in request.ScheduleTagIds)
        {
            if (scheduleTags.All(t => t.TagId != id))
            {
                throw new NotFoundException<ScheduleTag>(id);
            }
        }
        
        var stepTags = await dbContext.StepTags
            .Where(t => request.FilterStepTagIds.Contains(t.TagId))
            .ToArrayAsync(cancellationToken);

        foreach (var id in request.FilterStepTagIds)
        {
            if (stepTags.All(t => t.TagId != id))
            {
                throw new NotFoundException<StepTag>(id);
            }
        }

        var schedule = new Schedule
        {
            JobId = request.JobId,
            ScheduleName = request.ScheduleName,
            CronExpression = request.CronExpression,
            IsEnabled = request.IsEnabled,
            DisallowConcurrentExecution = request.DisallowConcurrentExecution
        };
        
        foreach (var tag in scheduleTags) schedule.Tags.Add(tag);
        foreach (var tag in stepTags) schedule.TagFilter.Add(tag);
        
        schedule.EnsureDataAnnotationsValidated();

        await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);
        try
        {
            dbContext.Schedules.Add(schedule);
            await dbContext.SaveChangesAsync(cancellationToken);
            await scheduler.AddScheduleAsync(schedule);
            await transaction.CommitAsync(cancellationToken);
        }
        catch
        {
            await transaction.RollbackAsync(CancellationToken.None);
            throw;
        }

        return schedule;
    }
}