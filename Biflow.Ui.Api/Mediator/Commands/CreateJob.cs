namespace Biflow.Ui.Api.Mediator.Commands;

internal record CreateJobCommand(JobDto Job) : IRequest<Job>;

[UsedImplicitly]
internal class CreateJobCommandHandler(IDbContextFactory<AppDbContext> dbContextFactory)
    : IRequestHandler<CreateJobCommand, Job>
{
    public async Task<Job> Handle(CreateJobCommand request, CancellationToken cancellationToken)
    {
        await using var context = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        if (await context.Jobs.AnyAsync(j => j.JobId == request.Job.JobId, cancellationToken))
        {
            throw new PrimaryKeyException<Job>(request.Job.JobId);
        }
        var job = new Job
        {
            JobId = request.Job.JobId,
            JobName = request.Job.JobName,
            JobDescription = request.Job.JobDescription,
            ExecutionMode = request.Job.ExecutionMode,
            StopOnFirstError = request.Job.StopOnFirstError,
            MaxParallelSteps = request.Job.MaxParallelSteps,
            OvertimeNotificationLimitMinutes = request.Job.OvertimeNotificationLimitMinutes,
            TimeoutMinutes = request.Job.TimeoutMinutes,
            IsEnabled = request.Job.IsEnabled,
            IsPinned = request.Job.IsPinned
        };
        job.EnsureDataAnnotationsValidated();
        context.Jobs.Add(job);
        await context.SaveChangesAsync(cancellationToken);
        return job;
    }
}