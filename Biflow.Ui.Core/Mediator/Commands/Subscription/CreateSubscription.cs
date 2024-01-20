namespace Biflow.Ui.Core;

public record CreateSubscriptionCommand(Subscription Subscription) : IRequest;

internal class CreateSubscriptionCommandHandler(IDbContextFactory<AppDbContext> dbContextFactory)
    : IRequestHandler<CreateSubscriptionCommand>
{
    public async Task Handle(CreateSubscriptionCommand request, CancellationToken cancellationToken)
    {
        using var context = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        context.Subscriptions.Add(request.Subscription);
        await context.SaveChangesAsync(cancellationToken);
    }
}