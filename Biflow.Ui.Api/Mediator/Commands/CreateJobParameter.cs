namespace Biflow.Ui.Api.Mediator.Commands;

internal record CreateJobParameterCommand(
    Guid JobId,
    string ParameterName,
    ParameterValue? ParameterValue,
    bool UseExpression,
    string? Expression) : IRequest<JobParameter>;

[UsedImplicitly]
internal class CreateJobParameterCommandHandler(
    IDbContextFactory<AppDbContext> dbContextFactory,
    JobValidator jobValidator) 
    : IRequestHandler<CreateJobParameterCommand, JobParameter>
{
    public async Task<JobParameter> Handle(CreateJobParameterCommand request, CancellationToken cancellationToken)
    {
        await using var dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        var job = await dbContext.Jobs
            .Include(j => j.JobParameters)
            .FirstOrDefaultAsync(j => j.JobId == request.JobId, cancellationToken)
            ?? throw new NotFoundException<Job>(request.JobId);
        var jobParameter = new JobParameter
        {
            JobId = request.JobId,
            ParameterName = request.ParameterName,
            ParameterValue = request.ParameterValue ?? new ParameterValue(),
            UseExpression = request.UseExpression,
            Expression = new EvaluationExpression { Expression = request.Expression }
        };
        jobParameter.EnsureDataAnnotationsValidated();
        job.JobParameters.Add(jobParameter);
        jobValidator.EnsureValidated(job);
        await dbContext.SaveChangesAsync(cancellationToken);
        return jobParameter;
    }
}