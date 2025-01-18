namespace Biflow.Ui.Api.Mediator.Commands;

internal record UpdateJobParameterCommand(
    Guid ParameterId,
    string ParameterName,
    ParameterValue? ParameterValue,
    bool UseExpression,
    string? Expression) : IRequest<JobParameter>;

[UsedImplicitly]
internal class UpdateJobParameterCommandHandler(
    IDbContextFactory<AppDbContext> dbContextFactory,
    JobValidator jobValidator)
    : IRequestHandler<UpdateJobParameterCommand, JobParameter>
{
    public async Task<JobParameter> Handle(UpdateJobParameterCommand request, CancellationToken cancellationToken)
    {
        await using var dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        var jobParameter = await dbContext.Set<JobParameter>()
            .FirstOrDefaultAsync(x => x.ParameterId == request.ParameterId, cancellationToken)
            ?? throw new NotFoundException<JobParameter>(request.ParameterId);
        var job = await dbContext.Jobs
            .Include(j => j.JobParameters)
            .FirstOrDefaultAsync(j => j.JobId == jobParameter.JobId, cancellationToken)
            ?? throw new NotFoundException<Job>(jobParameter.JobId);
        jobParameter.ParameterName = request.ParameterName;
        jobParameter.ParameterValue = request.ParameterValue ?? new ParameterValue();
        jobParameter.UseExpression = request.UseExpression;
        jobParameter.Expression.Expression = request.Expression;
        jobParameter.EnsureDataAnnotationsValidated();
        jobValidator.EnsureValidated(job);
        await dbContext.SaveChangesAsync(cancellationToken);
        return jobParameter;
    }
}