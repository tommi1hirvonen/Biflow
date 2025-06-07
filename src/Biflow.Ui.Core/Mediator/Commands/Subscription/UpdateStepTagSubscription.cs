namespace Biflow.Ui.Core;

public record UpdateStepTagSubscriptionCommand(
    Guid SubscriptionId,
    AlertType AlertType) : IRequest<StepTagSubscription>;

[UsedImplicitly]
internal class UpdateStepTagSubscriptionCommandHandler(IDbContextFactory<AppDbContext> dbContextFactory)
    : IRequestHandler<UpdateStepTagSubscriptionCommand, StepTagSubscription>
{
    public async Task<StepTagSubscription> Handle(UpdateStepTagSubscriptionCommand request,
        CancellationToken cancellationToken)
    {
        await using var dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        var subscription = await dbContext.StepTagSubscriptions
            .Include(s => s.Tag)
            .Include(s => s.User)
            .FirstOrDefaultAsync(s => s.SubscriptionId == request.SubscriptionId, cancellationToken)
            ?? throw new NotFoundException<StepTagSubscription>(request.SubscriptionId);
        
        subscription.AlertType = request.AlertType;
        
        subscription.EnsureDataAnnotationsValidated();
        
        await dbContext.SaveChangesAsync(cancellationToken);
        
        return subscription;
    }
}