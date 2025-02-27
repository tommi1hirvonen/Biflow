namespace Biflow.Ui.Core;

public record UpdateJobSubscriptionCommand(
    Guid SubscriptionId,
    AlertType? AlertType,
    bool NotifyOnOvertime) : IRequest<JobSubscription>;

[UsedImplicitly]
internal class UpdateJobSubscriptionCommandHandler(IDbContextFactory<AppDbContext> dbContextFactory)
    : IRequestHandler<UpdateJobSubscriptionCommand, JobSubscription>
{
    public async Task<JobSubscription> Handle(UpdateJobSubscriptionCommand request, CancellationToken cancellationToken)
    {
        await using var dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        var subscription = await dbContext.JobSubscriptions
            .Include(s => s.Job)
            .Include(s => s.User)
            .FirstOrDefaultAsync(s => s.SubscriptionId == request.SubscriptionId, cancellationToken)
            ?? throw new NotFoundException<JobSubscription>(request.SubscriptionId);
        
        subscription.NotifyOnOvertime = request.NotifyOnOvertime;
        subscription.AlertType = request.AlertType;
        
        subscription.EnsureDataAnnotationsValidated();
        
        await dbContext.SaveChangesAsync(cancellationToken);
        
        return subscription;
    }
}