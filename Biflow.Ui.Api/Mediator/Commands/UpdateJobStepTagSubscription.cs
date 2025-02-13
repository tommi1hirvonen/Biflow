namespace Biflow.Ui.Api.Mediator.Commands;

public record UpdateJobStepTagSubscriptionCommand(
    Guid SubscriptionId,
    AlertType AlertType) : IRequest<JobStepTagSubscription>;

[UsedImplicitly]
internal class UpdateJobStepTagSubscriptionCommandHandler(IDbContextFactory<AppDbContext> dbContextFactory)
    : IRequestHandler<UpdateJobStepTagSubscriptionCommand, JobStepTagSubscription>
{
    public async Task<JobStepTagSubscription> Handle(UpdateJobStepTagSubscriptionCommand request,
        CancellationToken cancellationToken)
    {
        await using var dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        var subscription = await dbContext.JobStepTagSubscriptions
            .Include(s => s.Job)
            .Include(s => s.Tag)
            .Include(s => s.User)
            .FirstOrDefaultAsync(s => s.SubscriptionId == request.SubscriptionId, cancellationToken)
            ?? throw new NotFoundException<JobSubscription>(request.SubscriptionId);
        
        subscription.AlertType = request.AlertType;
        
        subscription.EnsureDataAnnotationsValidated();
        
        await dbContext.SaveChangesAsync(cancellationToken);
        
        return subscription;
    }
}