namespace Biflow.Ui.Core;

public record UpdateSubscriptionCommand(Subscription Subscription) : IRequest;

internal class UpdateSubscriptionCommandHandler(IDbContextFactory<AppDbContext> dbContextFactory)
    : IRequestHandler<UpdateSubscriptionCommand>
{
    public async Task Handle(UpdateSubscriptionCommand request, CancellationToken cancellationToken)
    {
        await using var context = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        context.Attach(request.Subscription).State = EntityState.Modified;
        await context.SaveChangesAsync(cancellationToken);
    }
}