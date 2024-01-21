namespace Biflow.Ui.Core;

public record DeleteSubscriptionCommand(Guid SubscriptionId) : IRequest;

internal class DeleteSubscriptionCommandHandler(IDbContextFactory<AppDbContext> dbContextFactory)
    : IRequestHandler<DeleteSubscriptionCommand>
{
    public async Task Handle(DeleteSubscriptionCommand request, CancellationToken cancellationToken)
    {
        using var context = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        var sub = await context.Subscriptions
            .FirstOrDefaultAsync(s => s.SubscriptionId == request.SubscriptionId, cancellationToken);
        if (sub is not null)
        {
            context.Subscriptions.Remove(sub);
            await context.SaveChangesAsync(cancellationToken);
        }
    }
}