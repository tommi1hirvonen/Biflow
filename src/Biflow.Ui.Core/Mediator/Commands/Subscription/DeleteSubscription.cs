namespace Biflow.Ui.Core;

public record DeleteSubscriptionCommand(Guid SubscriptionId) : IRequest;

[UsedImplicitly]
internal class DeleteSubscriptionCommandHandler(IDbContextFactory<AppDbContext> dbContextFactory)
    : IRequestHandler<DeleteSubscriptionCommand>
{
    public async Task Handle(DeleteSubscriptionCommand request, CancellationToken cancellationToken)
    {
        await using var context = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        var sub = await context.Subscriptions
            .FirstOrDefaultAsync(s => s.SubscriptionId == request.SubscriptionId, cancellationToken)
            ?? throw new NotFoundException<Subscription>(request.SubscriptionId);
        context.Subscriptions.Remove(sub);
        await context.SaveChangesAsync(cancellationToken);
    }
}