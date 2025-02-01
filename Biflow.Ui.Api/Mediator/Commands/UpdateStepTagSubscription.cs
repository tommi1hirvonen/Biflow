namespace Biflow.Ui.Api.Mediator.Commands;

public record UpdateStepTagSubscriptionCommand(
    Guid SubscriptionId,
    AlertType AlertType) : IRequest<TagSubscription>;

[UsedImplicitly]
internal class UpdateStepTagSubscriptionCommandHandler(IDbContextFactory<AppDbContext> dbContextFactory)
    : IRequestHandler<UpdateStepTagSubscriptionCommand, TagSubscription>
{
    public async Task<TagSubscription> Handle(UpdateStepTagSubscriptionCommand request,
        CancellationToken cancellationToken)
    {
        await using var dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        var subscription = await dbContext.TagSubscriptions
            .Include(s => s.Tag)
            .Include(s => s.User)
            .FirstOrDefaultAsync(s => s.SubscriptionId == request.SubscriptionId, cancellationToken)
            ?? throw new NotFoundException<TagSubscription>(request.SubscriptionId);
        
        subscription.AlertType = request.AlertType;
        
        subscription.EnsureDataAnnotationsValidated();
        
        await dbContext.SaveChangesAsync(cancellationToken);
        
        return subscription;
    }
}