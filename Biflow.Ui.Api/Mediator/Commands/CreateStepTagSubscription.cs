namespace Biflow.Ui.Api.Mediator.Commands;

public record CreateStepTagSubscriptionCommand(
    Guid UserId,
    Guid StepTagId,
    AlertType AlertType) : IRequest<StepTagSubscription>;

[UsedImplicitly]
internal class CreateStepTagSubscriptionCommandHandler(IDbContextFactory<AppDbContext> dbContextFactory)
    : IRequestHandler<CreateStepTagSubscriptionCommand, StepTagSubscription>
{
    public async Task<StepTagSubscription> Handle(CreateStepTagSubscriptionCommand request,
        CancellationToken cancellationToken)
    {
        await using var dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken);

        var user = await dbContext.Users.FirstOrDefaultAsync(u => u.UserId == request.UserId, cancellationToken)
                   ?? throw new NotFoundException<User>(request.UserId);
        var tag = await dbContext.StepTags.FirstOrDefaultAsync(t => t.TagId == request.StepTagId, cancellationToken)
                  ?? throw new NotFoundException<StepTag>(request.StepTagId);

        var subscription = new StepTagSubscription(request.UserId, request.StepTagId)
        {
            User = user,
            Tag = tag,
            AlertType = request.AlertType
        };
        
        subscription.EnsureDataAnnotationsValidated();
        
        dbContext.StepTagSubscriptions.Add(subscription);
        await dbContext.SaveChangesAsync(cancellationToken);
        
        return subscription;
    }
}