namespace Biflow.Ui.Core;

public record UpdateSubscriptionCommand(Subscription Subscription) : IRequest;

internal class UpdateSubscriptionCommandHandler(IDbContextFactory<ServiceDbContext> dbContextFactory)
    : IRequestHandler<UpdateSubscriptionCommand>
{
    public async Task Handle(UpdateSubscriptionCommand request, CancellationToken cancellationToken)
    {
        using var context = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        context.Attach(request.Subscription).State = EntityState.Modified;
        await context.SaveChangesAsync(cancellationToken);
    }
}