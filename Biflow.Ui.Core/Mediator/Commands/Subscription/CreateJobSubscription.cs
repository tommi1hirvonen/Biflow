namespace Biflow.Ui.Core;

public record CreateJobSubscriptionCommand(
    Guid UserId,
    Guid JobId,
    AlertType? AlertType,
    bool NotifyOnOvertime) : IRequest<JobSubscription>;

[UsedImplicitly]
internal class CreateJobSubscriptionCommandHandler(IDbContextFactory<AppDbContext> dbContextFactory)
    : IRequestHandler<CreateJobSubscriptionCommand, JobSubscription>
{
    public async Task<JobSubscription> Handle(CreateJobSubscriptionCommand request, CancellationToken cancellationToken)
    {
        await using var dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken);

        var user = await dbContext.Users
            .FirstOrDefaultAsync(u => u.UserId == request.UserId, cancellationToken)
            ?? throw new NotFoundException<User>(request.UserId);
        
        var job = await dbContext.Jobs
            .FirstOrDefaultAsync(j => j.JobId == request.JobId, cancellationToken)
            ?? throw new NotFoundException<Job>(request.JobId);

        var subscription = new JobSubscription(request.UserId, request.JobId)
        {
            User = user,
            Job = job,
            AlertType = request.AlertType,
            NotifyOnOvertime = request.NotifyOnOvertime
        };
        
        subscription.EnsureDataAnnotationsValidated();
        
        dbContext.JobSubscriptions.Add(subscription);
        await dbContext.SaveChangesAsync(cancellationToken);
        
        return subscription;
    }
}