namespace Biflow.Ui.Api.Mediator.Commands;

public record CreateJobStepTagSubscriptionCommand(
    Guid UserId,
    Guid JobId,
    Guid StepTagId,
    AlertType AlertType) : IRequest<JobStepTagSubscription>;

[UsedImplicitly]
internal class CreateJobStepTagSubscriptionCommandHandler(IDbContextFactory<AppDbContext> dbContextFactory)
    : IRequestHandler<CreateJobStepTagSubscriptionCommand, JobStepTagSubscription>
{
    public async Task<JobStepTagSubscription> Handle(CreateJobStepTagSubscriptionCommand request,
        CancellationToken cancellationToken)
    {
        await using var dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken);

        var user = await dbContext.Users.FirstOrDefaultAsync(u => u.UserId == request.UserId, cancellationToken)
                   ?? throw new NotFoundException<User>(request.UserId);
        var job = await dbContext.Jobs.FirstOrDefaultAsync(j => j.JobId == request.JobId, cancellationToken)
                  ?? throw new NotFoundException<Job>(request.JobId);
        var tag = await dbContext.StepTags.FirstOrDefaultAsync(t => t.TagId == request.StepTagId, cancellationToken)
                  ?? throw new NotFoundException<StepTag>(request.StepTagId);

        var subscription = new JobStepTagSubscription(request.UserId, request.JobId, request.StepTagId)
        {
            User = user,
            Job = job,
            Tag = tag,
            AlertType = request.AlertType
        };
        
        subscription.EnsureDataAnnotationsValidated();
        
        dbContext.JobStepTagSubscriptions.Add(subscription);
        await dbContext.SaveChangesAsync(cancellationToken);
        
        return subscription;
    }
}