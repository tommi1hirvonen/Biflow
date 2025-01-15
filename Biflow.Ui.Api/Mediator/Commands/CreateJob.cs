namespace Biflow.Ui.Api.Mediator.Commands;

internal record CreateJobCommand(
    string JobName,
    string JobDescription,
    ExecutionMode ExecutionMode,
    bool StopOnFirstError,
    int MaxParallelSteps,
    double OvertimeNotificationLimitMinutes,
    double TimeoutMinutes,
    bool IsEnabled,
    bool IsPinned) : IRequest<Job>;

[UsedImplicitly]
internal class CreateJobCommandHandler(IDbContextFactory<AppDbContext> dbContextFactory)
    : IRequestHandler<CreateJobCommand, Job>
{
    public async Task<Job> Handle(CreateJobCommand request, CancellationToken cancellationToken)
    {
        await using var context = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        var job = new Job
        {
            JobName = request.JobName,
            JobDescription = request.JobDescription,
            ExecutionMode = request.ExecutionMode,
            StopOnFirstError = request.StopOnFirstError,
            MaxParallelSteps = request.MaxParallelSteps,
            OvertimeNotificationLimitMinutes = request.OvertimeNotificationLimitMinutes,
            TimeoutMinutes = request.TimeoutMinutes,
            IsEnabled = request.IsEnabled,
            IsPinned = request.IsPinned
        };
        job.EnsureDataAnnotationsValidated();
        context.Jobs.Add(job);
        await context.SaveChangesAsync(cancellationToken);
        return job;
    }
}