namespace Biflow.Ui.Core;

public record DeleteJobCommand(Guid JobId) : IRequest;

internal class DeleteJobCommandHandler(
    IDbContextFactory<AppDbContext> dbContextFactory,
    ISchedulerService scheduler)
    : IRequestHandler<DeleteJobCommand>
{
    public async Task Handle(DeleteJobCommand request, CancellationToken cancellationToken)
    {
        await using var context = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        
        // Manually controlling transactions is allowed since AppDbContext does not use retry-on-failure execution strategy.
        await using var transaction = context.Database.BeginTransaction();
        
        var jobToRemove = await context.Jobs
            .Include(j => j.JobParameters)
            .ThenInclude(j => j.AssigningStepParameters)
            .ThenInclude(p => p.Step)
            .Include(j => j.Steps)
            .ThenInclude(s => s.Dependencies)
            .Include(j => j.Steps)
            .ThenInclude(s => s.Depending)
            .Include($"{nameof(Job.Steps)}.{nameof(IHasStepParameters.StepParameters)}")
            .Include(j => j.JobSteps)
            .FirstOrDefaultAsync(j => j.JobId == request.JobId, cancellationToken);
        if (jobToRemove is not null)
        {
            context.Jobs.Remove(jobToRemove);
        }
        await context.SaveChangesAsync(cancellationToken);
        try
        {
            await scheduler.DeleteJobAsync(request.JobId);
            transaction.Commit();
        }
        catch
        {
            transaction.Rollback();
            throw;
        }
    }
}