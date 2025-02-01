namespace Biflow.Ui.Api.Mediator.Commands;

public record CreateStepSubscriptionCommand(
    Guid UserId,
    Guid StepId,
    AlertType AlertType) : IRequest<StepSubscription>;

[UsedImplicitly]
internal class CreateStepSubscriptionCommandHandler(IDbContextFactory<AppDbContext> dbContextFactory)
    : IRequestHandler<CreateStepSubscriptionCommand, StepSubscription>
{
    public async Task<StepSubscription> Handle(CreateStepSubscriptionCommand request, CancellationToken cancellationToken)
    {
        await using var dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        
        var user = await dbContext.Users.FirstOrDefaultAsync(u => u.UserId == request.UserId, cancellationToken)
                   ?? throw new NotFoundException<User>(request.UserId);
        var step = await dbContext.Steps.FirstOrDefaultAsync(s => s.StepId == request.StepId, cancellationToken)
                  ?? throw new NotFoundException<Step>(request.StepId);

        var subscription = new StepSubscription(request.UserId, request.StepId)
        {
            User = user,
            Step = step,
            AlertType = request.AlertType
        };
        
        subscription.EnsureDataAnnotationsValidated();
        
        dbContext.StepSubscriptions.Add(subscription);
        await dbContext.SaveChangesAsync(cancellationToken);
        
        return subscription;
    }
}