namespace Biflow.Ui.Core;

public record UpdateStepSubscriptionCommand(
    Guid SubscriptionId,
    AlertType AlertType) : IRequest<StepSubscription>;

[UsedImplicitly]
internal class UpdateStepSubscriptionCommandHandler(IDbContextFactory<AppDbContext> dbContextFactory)
    : IRequestHandler<UpdateStepSubscriptionCommand, StepSubscription>
{
    public async Task<StepSubscription> Handle(UpdateStepSubscriptionCommand request, CancellationToken cancellationToken)
    {
        await using var dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        var subscription = await dbContext.StepSubscriptions
            .Include(s => s.Step)
            .Include(s => s.User)
            .FirstOrDefaultAsync(s => s.SubscriptionId == request.SubscriptionId, cancellationToken)
            ?? throw new NotFoundException<StepSubscription>(request.SubscriptionId);
        
        subscription.AlertType = request.AlertType;
        
        subscription.EnsureDataAnnotationsValidated();
        
        await dbContext.SaveChangesAsync(cancellationToken);
        
        return subscription;
    }
}