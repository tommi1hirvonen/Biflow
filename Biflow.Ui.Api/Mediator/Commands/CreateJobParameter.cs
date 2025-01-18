namespace Biflow.Ui.Api.Mediator.Commands;

internal record CreateJobParameterCommand(
    Guid JobId,
    string ParameterName,
    ParameterValue? ParameterValue,
    bool UseExpression,
    string? Expression) : IRequest<JobParameter>;

[UsedImplicitly]
internal class CreateJobParameterCommandHandler(IDbContextFactory<AppDbContext> dbContextFactory) 
    : IRequestHandler<CreateJobParameterCommand, JobParameter>
{
    public async Task<JobParameter> Handle(CreateJobParameterCommand request, CancellationToken cancellationToken)
    {
        var jobParameter = new JobParameter
        {
            JobId = request.JobId,
            ParameterName = request.ParameterName,
            ParameterValue = request.ParameterValue ?? new ParameterValue(),
            UseExpression = request.UseExpression,
            Expression = new EvaluationExpression { Expression = request.Expression }
        };
        jobParameter.EnsureDataAnnotationsValidated();
        await using var dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        dbContext.Add(jobParameter);
        await dbContext.SaveChangesAsync(cancellationToken);
        return jobParameter;
    }
}